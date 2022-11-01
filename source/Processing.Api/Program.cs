// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.Core.App.FunctionApp.Middleware;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Processing.Api.Configuration;
using Processing.Api.CustomerMasterData;
using Processing.Api.EventListeners;
using Processing.Api.Monitor;
using Processing.Api.MoveIn;
using Processing.Application.ChangeCustomerCharacteristics;
using Processing.Application.ChangeCustomerCharacteristics.Validation;
using Processing.Application.ChangeOfSupplier;
using Processing.Application.ChangeOfSupplier.Validation;
using Processing.Application.Common;
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.BusinessRequestProcessing.Pipeline;
using Processing.Infrastructure.Configuration;
using Processing.Infrastructure.Configuration.Correlation;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.DataAccess.AccountingPoints;
using Processing.Infrastructure.Configuration.DataAccess.EnergySuppliers;
using Processing.Infrastructure.Configuration.DomainEventDispatching;
using Processing.Infrastructure.Configuration.Serialization;
using Processing.Infrastructure.RequestAdapters;
using Processing.Infrastructure.Users;
using SimpleInjector;

[assembly: CLSCompliant(false)]

namespace Processing.Api
{
    public class Program : EntryPoint
    {
        public static async Task Main()
        {
            var program = new Program();

            var host = program.ConfigureApplication();
            program.AssertConfiguration();
            await program.ExecuteApplicationAsync(host).ConfigureAwait(false);
        }

        protected override void ConfigureFunctionsWorkerDefaults(IFunctionsWorkerApplicationBuilder options)
        {
            base.ConfigureFunctionsWorkerDefaults(options);

            options.UseMiddleware<CorrelationIdMiddleware>();
            options.UseMiddleware<EntryPointTelemetryScopeMiddleware>();
            options.UseMiddleware<ServiceBusActorContextMiddleware>();
        }

        protected override void ConfigureServiceCollection(IServiceCollection services)
        {
            base.ConfigureServiceCollection(services);

            services.AddDbContext<MarketRolesContext>(x =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")
                                       ?? throw new InvalidOperationException(
                                           "database connection string not found.");

                x.UseSqlServer(connectionString, y => y.UseNodaTime());
            });

            services.AddLiveHealthCheck();
            services.AddSqlServerHealthCheck(Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")!);
            services.AddExternalServiceBusTopicsHealthCheck(
                Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE")!,
                "consumer-moved-in");
            services.AddExternalServiceBusSubscriptionsHealthCheck(
                Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE")!,
                Environment.GetEnvironmentVariable("INTEGRATION_EVENT_TOPIC_NAME")!,
                Environment.GetEnvironmentVariable("MARKET_PARTICIPANT_CHANGED_ACTOR_CREATED_SUBSCRIPTION_NAME")!,
                Environment.GetEnvironmentVariable("METERING_POINT_CREATED_EVENT_ENERGY_SUPPLYING_SUBSCRIPTION_NAME")!);
            services.AddExternalDomainServiceBusQueuesHealthCheck(
                Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE")!,
                Environment.GetEnvironmentVariable("CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME")!,
                Environment.GetEnvironmentVariable("CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME")!,
                Environment.GetEnvironmentVariable("CUSTOMER_MASTER_DATA_UPDATE_REQUEST_QUEUE_NAME")!);
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            SqlMapper.AddTypeHandler(InstantHandler.Default);

            container.AddOutbox();
            container.AddInternalCommandsProcessing();

            container.Register<CorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<EntryPointTelemetryScopeMiddleware>(Lifestyle.Scoped);
            container.Register<ServiceBusActorContextMiddleware>(Lifestyle.Scoped);
            container.Register<IActorContext, ActorContext>(Lifestyle.Scoped);
            container.Register<UserIdentityFactory>(Lifestyle.Singleton);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IAccountingPointRepository, AccountingPointRepository>(Lifestyle.Scoped);
            container.Register<IEnergySupplierRepository, EnergySupplierRepository>(Lifestyle.Scoped);
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Scoped);
            container.Register<IDomainEventsAccessor, DomainEventsAccessor>(Lifestyle.Scoped);
            container.Register<IDomainEventsDispatcher, DomainEventsDispatcher>(Lifestyle.Scoped);
            container.Register<MoveInHttpTrigger>(Lifestyle.Scoped);
            container.Register<SystemTimer>();
            container.Register<CustomerMasterDataRequestListener>();
            container.Register<ActorCreatedListener>();

            container.ConfigureMoveIn(16, 60, TimeOfDay.Create(0, 0, 0));

            container.Register<IValidator<ChangeCustomerMasterDataRequest>, InputValidationSet>(Lifestyle.Scoped);

            var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")
                                   ?? throw new InvalidOperationException(
                                       "database connection string not found.");
            container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(connectionString), Lifestyle.Scoped);

            container.BuildMediator(
                new[]
                {
                    ApplicationAssemblies.Application,
                    ApplicationAssemblies.Infrastructure,
                },
                new[]
                {
                    typeof(UnitOfWorkBehaviour<,>),
                    typeof(AuthorizationBehaviour<,>),
                    typeof(InputValidationBehaviour<,>),
                    typeof(DomainEventsDispatcherBehaviour<,>),
                    typeof(InternalCommandHandlingBehaviour<,>),
                });

            // Input validation(
            container.Register<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>(Lifestyle.Scoped);

            // Integration event publishing
            container.AddEventPublishing(
                Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND")!,
                Environment.GetEnvironmentVariable("INTEGRATION_EVENT_TOPIC_NAME")!);

            // Health check
            container.Register<HealthCheckEndpoint>(Lifestyle.Scoped);

            // Event listeners
            container.Register<MeteringPointCreatedListener>(Lifestyle.Scoped);

            // Master data request
            var serviceBusConnectionString =
                Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND");
            var queueName = Environment.GetEnvironmentVariable("CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME");
            container.Register<ServiceBusSender>(
                () =>
                {
                    var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
                    return serviceBusClient.CreateSender(queueName);
                },
                Lifestyle.Singleton);
        }
    }
}
