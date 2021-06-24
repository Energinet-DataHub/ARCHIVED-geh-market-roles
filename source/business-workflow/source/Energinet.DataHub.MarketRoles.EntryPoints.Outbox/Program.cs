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
using System.Reflection;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Application.Integration;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Handlers;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.MoveIn.Messages;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.Services;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Energinet.DataHub.MarketRoles.IntegrationEventContracts;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
                                             "Metering point db connection string not found.");

                x.UseSqlServer(dbConnectionString, options => options.UseNodaTime());
            });

            // services.RegisterProtoContracts<ConsumerRegisteredIntegrationEvent>();
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            // Register application components.
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);
            container.Register<IOutboxManager, OutboxManager>(Lifestyle.Scoped);
            container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Scoped);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<EventMessageDispatcher>(Lifestyle.Transient);
            container.Register<IIntegrationEventDispatchOrchestrator, IntegrationEventDispatchOrchestrator>(Lifestyle.Transient);

            var connectionString = Environment.GetEnvironmentVariable("SHARED_INTEGRATION_EVENT_SERVICE_BUS_SENDER_CONNECTION_STRING");
            container.Register<ServiceBusClient>(
                () => new ServiceBusClient(connectionString),
                Lifestyle.Singleton);

            container.Register(
                () => new ConsumerRegisteredTopic(Environment.GetEnvironmentVariable("CONSUMER_REGISTERED_TOPIC") ?? throw new InvalidOperationException(
                    "No Consumer Registered Topic found")),
                Lifestyle.Singleton);

            container.Register(typeof(ITopicSender<>), typeof(TopicSender<>), Lifestyle.Singleton);

            container.BuildMediator(
                new[]
                {
                    typeof(ConsumerMoveInAccepted).Assembly,
                    typeof(ConsumerMovedInEvent).Assembly,
                },
                Array.Empty<Type>());

            container.AddProtoBuffContracts(
                new[]
                {
                    typeof(ConsumerRegisteredIntegrationEvent).Assembly,
                });
        }
    }
}
