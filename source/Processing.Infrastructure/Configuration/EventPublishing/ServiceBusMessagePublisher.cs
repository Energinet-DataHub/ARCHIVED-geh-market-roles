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

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class ServiceBusMessagePublisher : IMessagePublisher
    {
        private readonly ServiceBusClient _serviceBusClient;

        public ServiceBusMessagePublisher(ServiceBusClient serviceBusClient)
        {
            _serviceBusClient = serviceBusClient;
        }

        public async Task PublishAsync(IMessage integrationEvent)
        {
            if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));
            var sender = _serviceBusClient.CreateSender(integrationEvent.GetType().Name);
            await sender.SendMessageAsync(CreateMessage(integrationEvent)).ConfigureAwait(false);
            await sender.DisposeAsync().ConfigureAwait(false);
        }

        private static ServiceBusMessage CreateMessage(IMessage integrationEvent)
        {
            var binaryData = new System.BinaryData(integrationEvent.ToByteArray());
            var serviceBusMessage = new ServiceBusMessage()
            {
                Body = binaryData,
                ContentType = $"application/octet-stream;charset=utf-8",
            };
            return serviceBusMessage;
        }
    }
}
