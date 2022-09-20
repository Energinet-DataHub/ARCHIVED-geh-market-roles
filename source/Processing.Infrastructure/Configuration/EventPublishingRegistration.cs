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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Processing.Application.Common;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.Configuration.Correlation;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus;
using Processing.Infrastructure.Configuration.EventPublishing.Protobuf;
using SimpleInjector;

namespace Processing.Infrastructure.Configuration
{
    public static class EventPublishingRegistration
    {
        public static void AddEventPublishing(this Container container, IServiceBusSenderFactory serviceBusSenderFactory, string publishEventsToTopic)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            RegisterCommonServices(container, publishEventsToTopic);
            container.Register<IServiceBusSenderFactory>(() => serviceBusSenderFactory, Lifestyle.Singleton);
        }

        public static void AddEventPublishing(this Container container, string serviceBusConnectionStringForIntegrationEvents, string publishEventsToTopic)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            RegisterCommonServices(container, publishEventsToTopic);
            container.Register<IServiceBusSenderFactory, ServiceBusSenderFactory>(Lifestyle.Singleton);
            container.RegisterSingleton<ServiceBusClient>(() => new ServiceBusClient(serviceBusConnectionStringForIntegrationEvents));
        }

        private static void RegisterCommonServices(Container container, string publishEventsToTopic)
        {
            RegisterIntegrationEvents(container);
            container.Register<IEventPublisher, EventPublisher>(Lifestyle.Scoped);
            container.Register<EventDispatcher>(Lifestyle.Scoped);
            container.Register<MessageParser>(Lifestyle.Singleton);
            container.Register(
                () =>
                    new ServiceBusMessageDispatcher(
                        container.GetInstance<IServiceBusSenderFactory>(),
                        container.GetInstance<ISystemDateTimeProvider>(),
                        container.GetInstance<ICorrelationContext>(),
                        container.GetInstance<IntegrationEventMapper>(),
                        publishEventsToTopic),
                Lifestyle.Scoped);
        }

        private static void RegisterIntegrationEvents(Container container)
        {
            var mapper = new IntegrationEventMapper();
            mapper.Add("ConsumerMovedIn", typeof(ConsumerMovedIn), 1, "consumer-moved-in");
            mapper.Add("EnergySupplierChanged", typeof(EnergySupplierChanged), 1, "energy-supplier-changed");
            mapper.Add("FutureEnergySupplierChangeRegistered", typeof(FutureEnergySupplierChangeRegistered), 1, "energy-supplier-change-registered");

            container.Register<IntegrationEventMapper>(() => mapper, Lifestyle.Singleton);
        }
    }
}
