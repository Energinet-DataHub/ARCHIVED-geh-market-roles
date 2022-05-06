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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Common;
using Energinet.DataHub.MarketRoles.Messaging;
using Energinet.DataHub.MessageHub.Client;
using Energinet.DataHub.MessageHub.Client.SimpleInjector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Processing.Domain.SeedWork;
using Processing.Infrastructure;
using Processing.Infrastructure.Correlation;
using Processing.Infrastructure.DataAccess;
using Processing.Infrastructure.DataAccess.MessageHub;
using Processing.Infrastructure.Integration;
using Processing.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Processing.Infrastructure.Integration.IntegrationEvents.FutureEnergySupplierChangeRegistered;
using Processing.Infrastructure.LocalMessageHub;
using Processing.Infrastructure.Outbox;
using Processing.Infrastructure.Serialization;
using Processing.Infrastructure.Transport.Protobuf.Integration;
using SimpleInjector;

[assembly: CLSCompliant(false)]

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox
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

        protected override void ConfigureServiceCollection(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            base.ConfigureServiceCollection(services);

            services.AddDbContext<MarketRolesContext>(x =>
            {
                var dbConnectionString = Environment.GetEnvironmentVariable("MARKETROLES_DB_CONNECTION_STRING")
                                         ?? throw new InvalidOperationException(
                                             "Market roles db connection string not found.");

                x.UseSqlServer(dbConnectionString, options => options.UseNodaTime());
            });
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            // Register application components.
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            container.Register<IOutboxManager, OutboxManager>(Lifestyle.Scoped);
            container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Scoped);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<OutboxWatcher>(Lifestyle.Scoped);
            container.Register<OutboxOrchestrator>(Lifestyle.Scoped);
            container.Register<IOutboxMessageDispatcher, OutboxMessageDispatcher>(Lifestyle.Scoped);
            container.RegisterDecorator<IOutboxMessageDispatcher, OutboxMessageDispatcherTelemetryDecorator>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<IIntegrationMetadataContext, IntegrationMetadataContext>(Lifestyle.Scoped);
            container.Register<IIntegrationEventMessageFactory, IntegrationEventServiceBusMessageFactory>(Lifestyle.Scoped);

            var connectionString = Environment.GetEnvironmentVariable("SHARED_INTEGRATION_EVENT_SERVICE_BUS_SENDER_CONNECTION_STRING");
            container.Register<ServiceBusClient>(
                () => new ServiceBusClient(connectionString),
                Lifestyle.Singleton);

            container.Register(
                () => new EnergySupplierChangedTopic(Environment.GetEnvironmentVariable("ENERGY_SUPPLIER_CHANGED_TOPIC") ?? throw new InvalidOperationException(
                    "No EnergySupplierChanged Topic found")),
                Lifestyle.Singleton);

            container.Register(
                () => new FutureEnergySupplierChangeRegisteredTopic(Environment.GetEnvironmentVariable("ENERGY_SUPPLIER_CHANGE_REGISTERED_TOPIC") ?? throw new InvalidOperationException(
                    "No EnergySupplierChangeRegistered Topic found")),
                Lifestyle.Singleton);

            container.Register(typeof(ITopicSender<>), typeof(TopicSender<>), Lifestyle.Singleton);

            container.Register<MessageHubMessageFactory>(Lifestyle.Scoped);
            var messageHubStorageConnectionString = Environment.GetEnvironmentVariable("MESSAGEHUB_STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("MessageHub storage connection string not found.");
            var messageHubStorageContainerName = Environment.GetEnvironmentVariable("MESSAGEHUB_STORAGE_CONTAINER_NAME") ?? throw new InvalidOperationException("MessageHub storage container name not found.");
            var messageHubServiceBusConnectionString = Environment.GetEnvironmentVariable("MESSAGEHUB_QUEUE_CONNECTION_STRING") ?? throw new InvalidOperationException("MessageHub queue connection string not found.");
            var messageHubServiceBusDataAvailableQueue = Environment.GetEnvironmentVariable("MESSAGEHUB_DATA_AVAILABLE_QUEUE") ?? throw new InvalidOperationException("MessageHub data available queue not found.");
            var messageHubServiceBusDomainReplyQueue = Environment.GetEnvironmentVariable("MESSAGEHUB_DOMAIN_REPLY_QUEUE") ?? throw new InvalidOperationException("MessageHub reply queue not found.");
            container.AddMessageHubCommunication(
                messageHubServiceBusConnectionString,
                new MessageHubConfig(messageHubServiceBusDataAvailableQueue, messageHubServiceBusDomainReplyQueue),
                messageHubStorageConnectionString,
                new StorageConfig(messageHubStorageContainerName));
            container.Register<ILocalMessageHubDataAvailableClient, LocalMessageHubDataAvailableClient>(Lifestyle.Scoped);
            container.Register<IMessageHubMessageRepository, MessageHubMessageRepository>(Lifestyle.Scoped);
            container.Register<IOutboxDispatcher<DataAvailableNotification>, DataAvailableNotificationOutboxDispatcher>();
            container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);

            container.BuildMediator(
                new[] { typeof(OutboxWatcher).Assembly, },
                Array.Empty<Type>());

            container.SendProtobuf<Energinet.DataHub.MarketRoles.IntegrationEventContracts.EnergySupplierChanged>();
        }
    }
}
