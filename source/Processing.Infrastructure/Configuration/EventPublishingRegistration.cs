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
using Contracts.IntegrationEvents;
using Processing.Application.Common;
using Processing.Infrastructure.Configuration.EventPublishing;
using SimpleInjector;

namespace Processing.Infrastructure.Configuration
{
    public static class EventPublishingRegistration
    {
        public static void AddEventPublishing(this Container container, IMessagePublisher messagePublisher)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (messagePublisher == null) throw new ArgumentNullException(nameof(messagePublisher));

            RegisterCommonServices(container);
            container.Register<IMessagePublisher>(() => messagePublisher, Lifestyle.Singleton);
        }

        public static void AddEventPublishing(this Container container, string serviceBusConnectionStringForIntegrationEvents)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            RegisterCommonServices(container);
            container.Register<IMessagePublisher, ServiceBusMessagePublisher>(Lifestyle.Singleton);
            container.RegisterSingleton<ServiceBusClient>(() => new ServiceBusClient(serviceBusConnectionStringForIntegrationEvents));
        }

        private static void RegisterCommonServices(Container container)
        {
            RegisterIntegrationEvents(container);
            container.Register<IEventPublisher, EventPublisher>(Lifestyle.Scoped);
            container.Register<EventDispatcher>(Lifestyle.Scoped);
        }

        private static void RegisterIntegrationEvents(Container container)
        {
            var mapper = new IntegrationEventMapper();
            mapper.Add(nameof(Contracts.IntegrationEvents.ConsumerMovedIn), typeof(ConsumerMovedIn), 1, "consumer-moved-in");

            container.Register<IntegrationEventMapper>(() => mapper, Lifestyle.Singleton);
        }
    }
}
