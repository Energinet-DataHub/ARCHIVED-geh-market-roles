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
using Processing.Api.Monitor;
using Processing.Api.MoveIn;
using Processing.Application.ChangeOfSupplier;
using Processing.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Processing.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Processing.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Processing.Application.ChangeOfSupplier.Validation;
using Processing.Application.Common;
using Processing.Application.Common.Commands;
using Processing.Application.Common.DomainEvents;
using Processing.Application.Common.Processing;
using Processing.Application.EDI;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Validation;
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Processing.Domain.SeedWork;
using Processing.Infrastructure;
using Processing.Infrastructure.BusinessRequestProcessing;
using Processing.Infrastructure.BusinessRequestProcessing.Pipeline;
using Processing.Infrastructure.Configuration;
using Processing.Infrastructure.Configuration.Correlation;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.DataAccess.AccountingPoints;
using Processing.Infrastructure.Configuration.DataAccess.Consumers;
using Processing.Infrastructure.Configuration.DataAccess.EnergySuppliers;
using Processing.Infrastructure.Configuration.DataAccess.ProcessManagers;
using Processing.Infrastructure.Configuration.DomainEventDispatching;
using Processing.Infrastructure.Configuration.Serialization;
using Processing.Infrastructure.ContainerExtensions;
using Processing.Infrastructure.EDI;
using Processing.Infrastructure.EDI.ChangeOfSupplier.ConsumerDetails;
using Processing.Infrastructure.EDI.ChangeOfSupplier.EndOfSupplyNotification;
using Processing.Infrastructure.EDI.ChangeOfSupplier.MeteringPointDetails;
using Processing.Infrastructure.Integration.Notifications;
using Processing.Infrastructure.InternalCommands;
using Processing.Infrastructure.RequestAdapters;
using Processing.Infrastructure.Transport.Protobuf;
using Processing.Infrastructure.Transport.Protobuf.Integration;
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
                Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_MANAGE_FOR_INTEGRATION_EVENTS")!,
                "consumer-moved-in");
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            container.AddOutbox();

            container.Register<CorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<EntryPointTelemetryScopeMiddleware>(Lifestyle.Scoped);
            container.Register<ServiceBusActorContextMiddleware>(Lifestyle.Scoped);
            container.Register<IActorContext, ActorContext>(Lifestyle.Scoped);
            container.Register<IActorProvider, ActorProvider>(Lifestyle.Scoped);
            container.Register<UserIdentityFactory>(Lifestyle.Singleton);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IAccountingPointRepository, AccountingPointRepository>(Lifestyle.Scoped);
            container.Register<IEnergySupplierRepository, EnergySupplierRepository>(Lifestyle.Scoped);
            container.Register<IProcessManagerRepository, ProcessManagerRepository>(Lifestyle.Scoped);
            container.Register<IConsumerRepository, ConsumerRepository>(Lifestyle.Scoped);
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Scoped);
            container.Register<ICommandScheduler, CommandScheduler>(Lifestyle.Scoped);
            container.Register<IDomainEventsAccessor, DomainEventsAccessor>(Lifestyle.Scoped);
            container.Register<IDomainEventsDispatcher, DomainEventsDispatcher>(Lifestyle.Scoped);
            container.Register<IDomainEventPublisher, DomainEventPublisher>(Lifestyle.Scoped);
            container.Register<IProtobufMessageFactory, ProtobufMessageFactory>(Lifestyle.Singleton);
            container.Register<INotificationReceiver, NotificationReceiver>(Lifestyle.Scoped);
            container.Register<IntegrationEventReceiver>(Lifestyle.Scoped);
            container.Register<IActorMessageService, ActorMessageService>(Lifestyle.Scoped);
            container.Register<IMessageHubDispatcher, MessageHubDispatcher>(Lifestyle.Scoped);
            container.Register<MoveInHttpTrigger>(Lifestyle.Scoped);
            container.Register<JsonMoveInAdapter>(Lifestyle.Scoped);
            container.Register<SystemTimer>();

            container.ConfigureMoveInProcessTimePolicy(7, 60, TimeOfDay.Create(0, 0, 0));

            var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")
                                   ?? throw new InvalidOperationException(
                                       "database connection string not found.");
            container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(connectionString), Lifestyle.Scoped);

            container.BuildMediator(
                new[]
                {
                    typeof(RequestChangeOfSupplierHandler).Assembly,
                    typeof(PublishWhenEnergySupplierHasChanged).Assembly,
                },
                new[]
                {
                    typeof(UnitOfWorkBehaviour<,>),
                    typeof(AuthorizationBehaviour<,>),
                    typeof(InputValidationBehaviour<,>),
                    typeof(DomainEventsDispatcherBehaviour<,>),
                    typeof(InternalCommandHandlingBehaviour<,>),
                });

            container.ReceiveProtobuf<Energinet.DataHub.MarketRoles.Contracts.MarketRolesEnvelope>(
                config => config
                    .FromOneOf(envelope => envelope.MarketRolesMessagesCase)
                    .WithParser(() => Energinet.DataHub.MarketRoles.Contracts.MarketRolesEnvelope.Parser));

            container.SendProtobuf<Contracts.IntegrationEvents.EnergySupplierChanged>();

            // Actor Notification handlers
            container.Register<IEndOfSupplyNotifier, EndOfSupplyNotifier>(Lifestyle.Scoped);
            container.Register<IConsumerDetailsForwarder, ConsumerDetailsForwarder>(Lifestyle.Scoped);
            container.Register<IMeteringPointDetailsForwarder, MeteringPointDetailsForwarder>(Lifestyle.Scoped);

            // Input validation(
            container.Register<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<MoveInRequest>, InputValidationSet>(Lifestyle.Scoped);
            container.AddValidationErrorConversion(
                validateRegistrations: false,
                typeof(MoveInRequest).Assembly, // Application
                typeof(ConsumerMovedIn).Assembly, // Domain
                typeof(ErrorMessageFactory).Assembly); // Infrastructure

            // Integration event publishing
            container.AddEventPublishing(Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS")!);

            // Health check
            container.Register<HealthCheckEndpoint>(Lifestyle.Scoped);
        }
    }
}
