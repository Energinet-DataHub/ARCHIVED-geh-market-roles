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
using System.Threading.Tasks;
using Google.Protobuf;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus;
using Xunit;

namespace Processing.IntegrationTests.TestDoubles
{
    public sealed class ServiceBusSenderFactorySpy : IServiceBusSenderFactory
    {
        private readonly List<IServiceBusSenderAdapter> _senders = new();

        public IServiceBusSenderAdapter GetSender(string topicName)
        {
            var sender = _senders.FirstOrDefault(a => a.TopicName.Equals(topicName, StringComparison.OrdinalIgnoreCase));
            if (sender is null)
            {
                sender = new ServiceBusSenderSpy(topicName);
                _senders.Add(sender);
            }

            return sender;
        }

        #pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public async ValueTask DisposeAsync()
        {
            foreach (var serviceBusSenderAdapter in _senders)
            {
                await serviceBusSenderAdapter.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            foreach (var serviceBusSenderAdapter in _senders)
            {
                serviceBusSenderAdapter.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        internal void AssertPublishedMessage(EventMetadata metadata, IMessage integrationEvent)
        {
            var eventId = GetEventId(integrationEvent);
            var senderSpy = _senders.First() as ServiceBusSenderSpy;
            var message = senderSpy?.Message!;
            Assert.NotNull(message);
            Assert.Equal("application/octet-stream;charset=utf-8", message.ContentType);
            Assert.NotNull(message.Body);
            Assert.NotNull(message.ApplicationProperties["OperationTimestamp"]);
            Assert.Equal(metadata.Version, message.ApplicationProperties["MessageVersion"]);
            Assert.Equal(metadata.EventName, message.ApplicationProperties["MessageType"]);
            Assert.Equal(message.ApplicationProperties["EventIdentification"], eventId);
            Assert.NotNull(message.ApplicationProperties["OperationCorrelationId"]);
            Assert.Equal(message.MessageId, eventId);
        }

        private static string? GetEventId(IMessage integrationEvent)
        {
            return integrationEvent.Descriptor.FindFieldByName("id").Accessor.GetValue(integrationEvent).ToString();
        }
    }
}
