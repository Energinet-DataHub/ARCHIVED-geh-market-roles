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
using System.Data;
using System.Linq;
using Energinet.DataHub.MarketRoles.Application;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.DomainEvents;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.AccountingPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.ProcessManagers;
using Energinet.DataHub.MarketRoles.Infrastructure.DomainEventDispatching;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application
{
    [Collection("IntegrationTest")]
    public class TestHost : IDisposable
    {
        private SqlConnection _sqlConnection = null;
        private BusinessProcessId _businessProcessId = null;

        protected TestHost()
        {
            CleanupDatabase();

            var services = new ServiceCollection();

            services.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(ConnectionString, y => y.UseNodaTime()));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProviderStub>();
            services.AddScoped<IAccountingPointRepository, AccountingPointRepository>();
            services.AddScoped<IEnergySupplierRepository, EnergySupplierRepository>();
            services.AddScoped<IProcessManagerRepository, ProcessManagerRepository>();
            services.AddScoped<IConsumerRepository, ConsumerRepository>();
            services.AddScoped<IJsonSerializer, JsonSerializer>();
            services.AddScoped<IOutbox, OutboxProvider>();
            services.AddSingleton<IOutboxMessageFactory, OutboxMessageFactory>();
            services.AddScoped<ICommandScheduler, CommandScheduler>();
            services.AddScoped<IDomainEventsAccessor, DomainEventsAccessor>();
            services.AddScoped<IDomainEventsDispatcher, DomainEventsDispatcher>();
            services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
            services.AddScoped<IDbConnectionFactory>(sp => new SqlDbConnectionFactory(ConnectionString));

            services.AddMediatR(new[]
            {
                typeof(RequestChangeOfSupplierHandler).Assembly,
                typeof(PublishWhenEnergySupplierHasChanged).Assembly,
            });

            // Actor Notification handlers
            services.AddScoped<IEndOfSupplyNotifier, EndOfSupplyNotifier>();
            services.AddScoped<IConsumerDetailsForwarder, ConsumerDetailsForwarder>();
            services.AddScoped<IMeteringPointDetailsForwarder, MeteringPointDetailsForwarder>();

            // Busines process responders
            services.AddScoped<IBusinessProcessResponder<RequestChangeOfSupplier>, RequestChangeOfSupplierResponder>();

            // Input validation
            services.AddScoped<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>();

            // Business process pipeline
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InputValidationBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DomainEventsDispatcherBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(BusinessProcessResponderBehaviour<,>));

            ServiceProvider = services.BuildServiceProvider();
            Mediator = ServiceProvider.GetRequiredService<IMediator>();
            AccountingPointRepository = ServiceProvider.GetRequiredService<IAccountingPointRepository>();
            EnergySupplierRepository = ServiceProvider.GetRequiredService<IEnergySupplierRepository>();
            ProcessManagerRepository = ServiceProvider.GetRequiredService<IProcessManagerRepository>();
            ConsumerRepository = ServiceProvider.GetRequiredService<IConsumerRepository>();
            UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
            MarketRolesContext = ServiceProvider.GetRequiredService<MarketRolesContext>();
            SystemDateTimeProvider = ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
            Serializer = ServiceProvider.GetRequiredService<IJsonSerializer>();
            SystemDateTimeProvider = new SystemDateTimeProviderStub();
            CommandScheduler = ServiceProvider.GetRequiredService<ICommandScheduler>();
            Transaction = new Transaction(Guid.NewGuid().ToString());
        }

        protected IServiceProvider ServiceProvider { get; }

        protected IMediator Mediator { get; }

        protected IAccountingPointRepository AccountingPointRepository { get; }

        protected IEnergySupplierRepository EnergySupplierRepository { get; }

        protected IConsumerRepository ConsumerRepository { get; }

        protected IProcessManagerRepository ProcessManagerRepository { get; }

        protected IUnitOfWork UnitOfWork { get; }

        protected ISystemDateTimeProvider SystemDateTimeProvider { get; }

        protected MarketRolesContext MarketRolesContext { get; }

        protected ICommandScheduler CommandScheduler { get; }

        protected IJsonSerializer Serializer { get; }

        protected Transaction Transaction { get; }

        protected Instant EffectiveDate => SystemDateTimeProvider.Now();

        private string ConnectionString =>
            Environment.GetEnvironmentVariable("MarketData_IntegrationTests_ConnectionString");

        public void Dispose()
        {
            CleanupDatabase();
        }

        protected TService GetService<TService>()
        {
            return ServiceProvider.GetRequiredService<TService>();
        }

        protected SqlConnection GetSqlDbConnection()
        {
            if (_sqlConnection is null)
                _sqlConnection = new SqlConnection(ConnectionString);

            if (_sqlConnection.State == ConnectionState.Closed)
                _sqlConnection.Open();
            return _sqlConnection;
        }

        protected Consumer CreateConsumer()
        {
            var consumerId = new ConsumerId(Guid.NewGuid());
            var consumer = new Consumer(consumerId, CprNumber.Create(SampleData.ConsumerId));

            ConsumerRepository.Add(consumer);

            return consumer;
        }

        protected EnergySupplier CreateEnergySupplier()
        {
            var energySupplierId = new EnergySupplierId(Guid.NewGuid());
            var energySupplierGln = new GlnNumber(SampleData.GlnNumber);
            var energySupplier = new EnergySupplier(energySupplierId, energySupplierGln);
            EnergySupplierRepository.Add(energySupplier);
            return energySupplier;
        }

        protected AccountingPoint CreateAccountingPoint()
        {
            var meteringPoint =
                AccountingPoint.CreateProduction(
                    GsrnNumber.Create(SampleData.GsrnNumber), true);

            AccountingPointRepository.Add(meteringPoint);

            return meteringPoint;
        }

        protected Transaction CreateTransaction()
        {
            return new Transaction(Guid.NewGuid().ToString());
        }

        protected void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId)
        {
            var systemTimeProvider = ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            var transaction = CreateTransaction();
            SetConsumerMovedIn(accountingPoint, consumerId, energySupplierId, moveInDate, transaction);
        }

        protected void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, Transaction transaction)
        {
            var systemTimeProvider = ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, transaction);
            accountingPoint.EffectuateConsumerMoveIn(transaction, systemTimeProvider);
        }

        protected void RegisterChangeOfSupplier(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId, Transaction transaction)
        {
            var systemTimeProvider = ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();

            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            var changeSupplierDate = systemTimeProvider.Now();

            SetConsumerMovedIn(accountingPoint, consumerId, energySupplierId);
            accountingPoint.AcceptChangeOfSupplier(energySupplierId, changeSupplierDate, transaction, systemTimeProvider);
        }

        protected BusinessProcessId GetBusinessProcessId()
        {
            if (_businessProcessId == null)
            {
                var command = new SqlCommand($"SELECT Id FROM [dbo].[BusinessProcesses] WHERE TransactionId = '{Transaction.Value}'", GetSqlDbConnection());
                var id = command.ExecuteScalar();
                _businessProcessId = new BusinessProcessId(Guid.Parse(id.ToString()));
            }

            return _businessProcessId;
        }

        protected BusinessProcessId GetBusinessProcessId(Transaction transaction)
        {
            if (_businessProcessId == null)
            {
                var connection = new SqlConnection(ConnectionString);
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                var command = new SqlCommand($"SELECT Id FROM [dbo].[BusinessProcesses] WHERE TransactionId = '{transaction.Value}'", connection);
                var id = command.ExecuteScalar();
                _businessProcessId = new BusinessProcessId(Guid.Parse(id.ToString()));
            }

            return _businessProcessId;
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

            new SqlCommand(cleanupStatement, GetSqlDbConnection()).ExecuteNonQuery();
        }
    }
}
