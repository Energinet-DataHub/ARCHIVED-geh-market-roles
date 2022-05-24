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
using Processing.Application.Common;
using Processing.Infrastructure.Configuration.Outbox;

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IOutbox _outbox;
        private readonly IOutboxMessageFactory _outboxMessageFactory;

        public EventPublisher(IOutbox outbox, IOutboxMessageFactory outboxMessageFactory)
        {
            _outbox = outbox;
            _outboxMessageFactory = outboxMessageFactory;
        }

        public Task PublishAsync<TEvent>(TEvent integrationEvent)
        {
            if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));
            var message = integrationEvent.ToString() ?? throw new InvalidCastException("Message cannot be empty.");
            var messageType = integrationEvent.GetType().FullName ?? throw new InvalidCastException("Message type cannot be empty.");

            _outbox.Add(_outboxMessageFactory.CreateFrom(message, messageType, OutboxMessageCategory.IntegrationEvent));
            return Task.CompletedTask;
        }
    }
}
