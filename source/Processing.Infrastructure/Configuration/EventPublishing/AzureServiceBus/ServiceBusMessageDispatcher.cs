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
using Google.Protobuf;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.Configuration.Correlation;

namespace Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus
{
    public class ServiceBusMessageDispatcher
    {
        private readonly IServiceBusSenderFactory _serviceBusSenderFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly ICorrelationContext _correlationContext;
        private readonly IntegrationEventMapper _integrationEventMapper;

        public ServiceBusMessageDispatcher(IServiceBusSenderFactory serviceBusSenderFactory, ISystemDateTimeProvider systemDateTimeProvider, ICorrelationContext correlationContext, IntegrationEventMapper integrationEventMapper)
        {
            _serviceBusSenderFactory = serviceBusSenderFactory;
            _systemDateTimeProvider = systemDateTimeProvider;
            _correlationContext = correlationContext;
            _integrationEventMapper = integrationEventMapper;
        }

        public Task DispatchAsync(IMessage integrationEvent)
        {
            if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));
            var eventMetadata = _integrationEventMapper.GetByType(integrationEvent.GetType());
            if (eventMetadata == null)
            {
                throw new InvalidOperationException($"Could not find metadata for integration event type {integrationEvent.GetType().Name}");
            }

            return _serviceBusSenderFactory.GetSender(eventMetadata.TopicName).SendAsync(CreateMessage(integrationEvent, eventMetadata));
        }

        private ServiceBusMessage CreateMessage(IMessage integrationEvent, EventMetadata eventMetadata)
        {
            var serviceBusMessage = new ServiceBusMessage();
            serviceBusMessage.Body = new BinaryData(integrationEvent.ToByteArray());
            serviceBusMessage.ContentType = "application/octet-stream;charset=utf-8";
            serviceBusMessage.CorrelationId = _correlationContext.Id;
            serviceBusMessage.ApplicationProperties.Add("OperationTimestamp", _systemDateTimeProvider.Now().ToDateTimeUtc());
            serviceBusMessage.ApplicationProperties.Add("MessageVersion", eventMetadata.Version);
            serviceBusMessage.ApplicationProperties.Add("MessageType", eventMetadata.EventName);
            serviceBusMessage.ApplicationProperties.Add("EventIdentification", Guid.NewGuid());
            return serviceBusMessage;
        }
    }
}
