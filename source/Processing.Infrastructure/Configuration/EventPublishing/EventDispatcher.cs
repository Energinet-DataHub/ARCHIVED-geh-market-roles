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
using System.Reflection;
using System.Threading.Tasks;
using Contracts.IntegrationEvents;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus;
using Processing.Infrastructure.Configuration.Outbox;

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class EventDispatcher
    {
        private readonly IOutboxManager _outboxManager;

        private readonly ServiceBusMessageDispatcher _messageDispatcher;

        public EventDispatcher(IOutboxManager outboxManager, ServiceBusMessageDispatcher messageDispatcher)
        {
            _outboxManager = outboxManager;
            _messageDispatcher = messageDispatcher;
        }

        public async Task DispatchAsync()
        {
            OutboxMessage? message;
            while ((message = _outboxManager.GetNext(OutboxMessageCategory.IntegrationEvent)) != null)
            {
                var integrationEvent = Protobuf.MessageParser.GetFrom(message.Type, message.Data);

                await _messageDispatcher.DispatchAsync(integrationEvent).ConfigureAwait(false);
                await _outboxManager.MarkProcessedAsync(message).ConfigureAwait(false);
            }
        }
    }
}
