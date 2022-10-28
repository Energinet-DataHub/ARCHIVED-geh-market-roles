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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using FluentValidation;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Processing.Application.ChangeCustomerCharacteristics;
using Processing.Application.ChangeOfSupplier;
using Processing.Application.ChangeOfSupplier.Validation;
using Processing.Application.Common;
using Processing.Application.Common.Commands;
using Processing.Application.Common.Queries;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Validation;
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
using Processing.Infrastructure.Configuration.InternalCommands;
using Processing.Infrastructure.Configuration.Serialization;
using Processing.Infrastructure.RequestAdapters;
using Processing.IntegrationTests.Application;
using Processing.IntegrationTests.Fixtures;
using Processing.IntegrationTests.TestDoubles;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using InputValidationSet = Processing.Application.ChangeCustomerCharacteristics.Validation.InputValidationSet;
using RequestChangeOfSupplier = Processing.Application.ChangeOfSupplier.RequestChangeOfSupplier;

namespace Processing.IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly Scope _scope;
        private readonly Container _container;
        private readonly ServiceBusSenderFactorySpy _serviceBusSenderFactorySpy;
        private bool _disposed;

        protected TestBase(DatabaseFixture databaseFixture)
        {
            if (databaseFixture == null)
                throw new ArgumentNullException(nameof(databaseFixture));
            databaseFixture.CleanupDatabase();

            _container = new Container();
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging();

            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            SqlMapper.AddTypeHandler(InstantHandler.Default);
            _container.AddOutbox();
            _container.AddInternalCommandsProcessing();

            serviceCollection.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(databaseFixture.ConnectionString, y => y.UseNodaTime()));
            serviceCollection.AddSimpleInjector(_container);
            var serviceProvider = serviceCollection.BuildServiceProvider().UseSimpleInjector(_container);

            _container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            _container.Register<IAccountingPointRepository, AccountingPointRepository>(Lifestyle.Scoped);
            _container.Register<IEnergySupplierRepository, EnergySupplierRepository>(Lifestyle.Scoped);
            _container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            _container.Register<ISystemDateTimeProvider, SystemDateTimeProviderStub>(Lifestyle.Singleton);
            _container.Register<IDomainEventsAccessor, DomainEventsAccessor>();
            _container.Register<IDomainEventsDispatcher, DomainEventsDispatcher>();
            _container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(databaseFixture.ConnectionString));
            _container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);

            _container.ConfigureMoveIn(0, 0, TimeOfDay.Create(0, 0, 0));

            _container.Register<IValidator<ChangeCustomerMasterDataRequest>, InputValidationSet>(Lifestyle.Scoped);

            // Integration event publishing
            _serviceBusSenderFactorySpy = new ServiceBusSenderFactorySpy();
            _container.AddEventPublishing(_serviceBusSenderFactorySpy, "Non_existing_topic");

            // Business process responders
            _container.Register<IActorContext>(() => new ActorContext { CurrentActor = new Actor(Guid.NewGuid(), "GLN", "8200000001409", "GridAccessProvider") }, Lifestyle.Singleton);

            // Input validation(
            _container.Register<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>(Lifestyle.Scoped);

            _container.BuildMediator(
                new[] { typeof(RequestChangeOfSupplierHandler).Assembly, typeof(PublishWhenEnergySupplierHasChanged).Assembly, },
                new[]
                {
                    typeof(UnitOfWorkBehaviour<,>), typeof(InputValidationBehaviour<,>), typeof(DomainEventsDispatcherBehaviour<,>),
                    typeof(InternalCommandHandlingBehaviour<,>),
                });

            _container.Register<ILogger>(() => NullLogger.Instance);
            _container.Register(() => new TelemetryClient(new TelemetryConfiguration()), Lifestyle.Scoped);

            _container.Verify();

            _scope = AsyncScopedLifestyle.BeginScope(_container);

            var correlationContext = _container.GetInstance<ICorrelationContext>();
            correlationContext.SetId(Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.Ordinal));
            correlationContext.SetParentId(Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.Ordinal)[..16]);

            ServiceProvider = serviceProvider;
            Mediator = _container.GetInstance<IMediator>();
            AccountingPointRepository = _container.GetInstance<IAccountingPointRepository>();
            EnergySupplierRepository = _container.GetInstance<IEnergySupplierRepository>();
            UnitOfWork = _container.GetInstance<IUnitOfWork>();
            MarketRolesContext = _container.GetInstance<MarketRolesContext>();
            SystemDateTimeProvider = _container.GetInstance<ISystemDateTimeProvider>();
            Serializer = _container.GetInstance<IJsonSerializer>();
            CommandScheduler = _container.GetInstance<ICommandScheduler>();
        }

        // TODO: Get rid of all properties and methods instead
        protected IServiceProvider ServiceProvider { get; }

        protected IMediator Mediator { get; }

        protected IAccountingPointRepository AccountingPointRepository { get; }

        protected IEnergySupplierRepository EnergySupplierRepository { get; }

        protected IUnitOfWork UnitOfWork { get; }

        protected ISystemDateTimeProvider SystemDateTimeProvider { get; }

        protected MarketRolesContext MarketRolesContext { get; }

        protected ICommandScheduler CommandScheduler { get; }

        protected IJsonSerializer Serializer { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _serviceBusSenderFactorySpy.Dispose();
            _scope.Dispose();
            _container.Dispose();

            _disposed = true;
        }

        protected TService GetService<TService>()
            where TService : class
        {
            return _container.GetInstance<TService>();
        }

        protected void SaveChanges()
        {
            GetService<MarketRolesContext>().SaveChanges();
        }

        protected async Task<BusinessProcessResult> SendRequestAsync(IBusinessRequest request)
        {
            return await GetService<IMediator>().Send(request, CancellationToken.None).ConfigureAwait(false);
        }

        protected Task InvokeCommandAsync(object command)
        {
            return GetService<IMediator>().Send(command, CancellationToken.None);
        }

        protected Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
        {
            return GetService<IMediator>().Send(query, CancellationToken.None);
        }

        protected Task<TCommand?> GetEnqueuedCommandAsync<TCommand>()
        {
            var commandMapper = GetService<InternalCommandMapper>();
            var commandMetadata = commandMapper.GetByType(typeof(TCommand));
            var queuedCommand = MarketRolesContext.QueuedInternalCommands
                .FirstOrDefault(queuedInternalCommand => queuedInternalCommand.Type == commandMetadata.CommandName);

            if (queuedCommand is null)
            {
                return Task.FromResult<TCommand?>(default);
            }

            var serializer = GetService<IJsonSerializer>();
            var command = (TCommand)serializer.Deserialize(queuedCommand.Data, commandMetadata.CommandType);
            return Task.FromResult<TCommand?>(command);
        }

        protected Domain.EnergySuppliers.EnergySupplier CreateEnergySupplier(Guid? id = null, string? glnNumber = null)
        {
            var energySupplierId = new EnergySupplierId(id ?? Guid.NewGuid());
            var energySupplierGln = new GlnNumber(glnNumber ?? SampleData.GlnNumber);
            var energySupplier = new Domain.EnergySuppliers.EnergySupplier(energySupplierId, energySupplierGln);
            EnergySupplierRepository.Add(energySupplier);
            return energySupplier;
        }

        protected AccountingPoint CreateAccountingPoint()
        {
            var meteringPoint =
                AccountingPoint.CreateProduction(
                    AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber), true);

            AccountingPointRepository.Add(meteringPoint);

            return meteringPoint;
        }
    }
}
