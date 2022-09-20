﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Processing.Application.ChangeOfSupplier;
using Processing.Application.ChangeOfSupplier.Validation;
using Processing.Application.Common;
using Processing.Application.Common.Commands;
using Processing.Application.Common.Queries;
using Processing.Application.EDI;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Validation;
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.BusinessRequestProcessing.Pipeline;
using Processing.Infrastructure.Configuration;
using Processing.Infrastructure.Configuration.Correlation;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.DataAccess.AccountingPoints;
using Processing.Infrastructure.Configuration.DataAccess.Consumers;
using Processing.Infrastructure.Configuration.DataAccess.EnergySuppliers;
using Processing.Infrastructure.Configuration.DomainEventDispatching;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.Infrastructure.Configuration.InternalCommands;
using Processing.Infrastructure.Configuration.Serialization;
using Processing.Infrastructure.ContainerExtensions;
using Processing.Infrastructure.EDI;
using Processing.Infrastructure.RequestAdapters;
using Processing.Infrastructure.Transport;
using Processing.Infrastructure.Transport.Protobuf.Integration;
using Processing.IntegrationTests.TestDoubles;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using Consumer = Processing.Domain.Consumers.Consumer;
using RequestChangeOfSupplier = Processing.Application.ChangeOfSupplier.RequestChangeOfSupplier;

namespace Processing.IntegrationTests.Application
{
    [Collection("IntegrationTest")]
#pragma warning disable CA1724 // TODO: TestHost is reserved. Maybe refactor to base EntryPoint?
    public class TestHost : IDisposable
    {
        private readonly Scope _scope;
        private readonly Container _container;
        private readonly string _connectionString;
        private readonly ServiceBusSenderFactorySpy _serviceBusSenderFactorySpy;
        private bool _disposed;
        private SqlConnection? _sqlConnection;

        protected TestHost(DatabaseFixture databaseFixture)
        {
            if (databaseFixture == null)
                throw new ArgumentNullException(nameof(databaseFixture));

            databaseFixture.DatabaseManager.UpgradeDatabase();
            _connectionString = databaseFixture.DatabaseManager.ConnectionString;

            _container = new Container();
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging();

            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            SqlMapper.AddTypeHandler(InstantHandler.Default);
            _container.AddOutbox();
            _container.AddInternalCommandsProcessing();

            _container.SendProtobuf<MarketRolesEnvelope>();
            _container.ReceiveProtobuf<MarketRolesEnvelope>(
                config => config
                    .FromOneOf(envelope => envelope.MarketRolesMessagesCase)
                    .WithParser(() => MarketRolesEnvelope.Parser));

            serviceCollection.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(_connectionString, y => y.UseNodaTime()));
            serviceCollection.AddSimpleInjector(_container);
            var serviceProvider = serviceCollection.BuildServiceProvider().UseSimpleInjector(_container);

            _container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            _container.Register<IAccountingPointRepository, AccountingPointRepository>(Lifestyle.Scoped);
            _container.Register<IEnergySupplierRepository, EnergySupplierRepository>(Lifestyle.Scoped);
            _container.Register<IConsumerRepository, ConsumerRepository>(Lifestyle.Scoped);
            _container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            _container.Register<ISystemDateTimeProvider, SystemDateTimeProviderStub>(Lifestyle.Singleton);
            _container.Register<IDomainEventsAccessor, DomainEventsAccessor>();
            _container.Register<IDomainEventsDispatcher, DomainEventsDispatcher>();
            _container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(_connectionString));
            _container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);

            _container.Register<JsonMoveInAdapter>(Lifestyle.Scoped);
            _container.ConfigureMoveInProcessTimePolicy(0, 0, TimeOfDay.Create(0, 0, 0));

            // Integration event publishing
            _serviceBusSenderFactorySpy = new ServiceBusSenderFactorySpy();
            _container.AddEventPublishing(_serviceBusSenderFactorySpy, "Non_existing_topic");

            // Business process responders
            _container.Register<IActorMessageService, ActorMessageService>(Lifestyle.Scoped);
            _container.Register<IMessageHubDispatcher, MessageHubDispatcher>(Lifestyle.Scoped);
            _container.Register<IActorContext>(() => new ActorContext { CurrentActor = new Actor(Guid.NewGuid(), "GLN", "8200000001409", "GridAccessProvider") }, Lifestyle.Singleton);

            // Input validation(
            _container.Register<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>(Lifestyle.Scoped);
            _container.Register<IValidator<MoveInRequest>, InputValidationSet>(Lifestyle.Scoped);
            _container.AddValidationErrorConversion(
                validateRegistrations: false,
                typeof(MoveInRequest).Assembly, // Application
                typeof(ConsumerMovedIn).Assembly, // Domain
                typeof(DocumentType).Assembly); // Infrastructure

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

            CleanupDatabase();

            ServiceProvider = serviceProvider;
            Mediator = _container.GetInstance<IMediator>();
            AccountingPointRepository = _container.GetInstance<IAccountingPointRepository>();
            EnergySupplierRepository = _container.GetInstance<IEnergySupplierRepository>();
            ConsumerRepository = _container.GetInstance<IConsumerRepository>();
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

        protected IConsumerRepository ConsumerRepository { get; }

        protected IUnitOfWork UnitOfWork { get; }

        protected ISystemDateTimeProvider SystemDateTimeProvider { get; }

        protected MarketRolesContext MarketRolesContext { get; }

        protected ICommandScheduler CommandScheduler { get; }

        protected IJsonSerializer Serializer { get; }

        protected Instant EffectiveDate => SystemDateTimeProvider.Now();

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

            CleanupDatabase();

            _serviceBusSenderFactorySpy.Dispose();
            _sqlConnection?.Dispose();
            _scope.Dispose();
            _container.Dispose();

            _disposed = true;
        }

        protected TService GetService<TService>()
            where TService : class
        {
            return _container.GetInstance<TService>();
        }

        protected SqlConnection GetSqlDbConnection()
        {
            if (_sqlConnection is null)
                _sqlConnection = new SqlConnection(_connectionString);
            if (_sqlConnection.State == ConnectionState.Closed)
                _sqlConnection.Open();
            return _sqlConnection;
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

        protected Consumer CreateConsumer()
        {
            var consumerId = new ConsumerId(Guid.NewGuid());
            var consumer = new Consumer(consumerId, CprNumber.Create(SampleData.ConsumerSSN), ConsumerName.Create(SampleData.ConsumerName));

            ConsumerRepository.Add(consumer);

            return consumer;
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

        protected void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId)
        {
            var systemTimeProvider = GetService<ISystemDateTimeProvider>();
            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            SetConsumerMovedIn(accountingPoint, consumerId, energySupplierId, moveInDate);
        }

        protected void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate)
        {
            if (accountingPoint == null)
                throw new ArgumentNullException(nameof(accountingPoint));

            var systemTimeProvider = GetService<ISystemDateTimeProvider>();
            var businessProcessId = BusinessProcessId.New();
            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, businessProcessId);
            accountingPoint.EffectuateConsumerMoveIn(businessProcessId, systemTimeProvider.Now());
        }

        protected void RegisterChangeOfSupplier(AccountingPoint accountingPoint, EnergySupplierId energySupplierId, BusinessProcessId processId)
        {
            if (accountingPoint == null)
                throw new ArgumentNullException(nameof(accountingPoint));

            var systemTimeProvider = GetService<ISystemDateTimeProvider>();

            var changeSupplierDate = systemTimeProvider.Now();

            accountingPoint.AcceptChangeOfSupplier(energySupplierId, changeSupplierDate, systemTimeProvider, processId);
        }

        protected IEnumerable<TMessage> GetOutboxMessages<TMessage>()
        {
            var jsonSerializer = GetService<IJsonSerializer>();
            var context = GetService<MarketRolesContext>();

            var messageType = GetIntegrationEventNameFromType<TMessage>() ?? typeof(TMessage).FullName;

            return context.OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => jsonSerializer.Deserialize<TMessage>(message.Data));
        }

        protected void AssertOutboxMessage<TMessage>()
        {
            var message = GetOutboxMessages<TMessage>().SingleOrDefault();

            message.Should().NotBeNull();
            message.Should().BeOfType<TMessage>();
        }

        private void CleanupDatabase()
        {
            var cleanupStatement = $"DELETE FROM [dbo].[ConsumerRegistrations] " +
                                   $"DELETE FROM [dbo].[SupplierRegistrations] " +
                                   $"DELETE FROM [dbo].[ProcessManagers] " +
                                   $"DELETE FROM [dbo].[BusinessProcesses] " +
                                   $"DELETE FROM [dbo].[Consumers] " +
                                   $"DELETE FROM [dbo].[EnergySuppliers] " +
                                   $"DELETE FROM [dbo].[AccountingPoints] " +
                                   $"DELETE FROM [dbo].[OutboxMessages] " +
                                   $"DELETE FROM [dbo].[QueuedInternalCommands]";

            using var sqlCommand = new SqlCommand(cleanupStatement, GetSqlDbConnection());
            sqlCommand.ExecuteNonQuery();
        }

        private string? GetIntegrationEventNameFromType<TIntegrationEventType>()
        {
            var mapper = GetService<IntegrationEventMapper>();
            try
            {
                return mapper.GetByType(typeof(TIntegrationEventType)).EventName;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
