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

using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MediatR;
using Processing.Infrastructure.Configuration.Outbox;
using Processing.Infrastructure.Configuration.SystemTime;

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class PublishEventsOnTimeHasPassed : INotificationHandler<TimeHasPassed>
    {
        private readonly IOutboxManager _outboxManager;

        private readonly IMessagePublisher _serviceBusMessagePublisher;

        public PublishEventsOnTimeHasPassed(IOutboxManager outboxManager, IMessagePublisher serviceBusMessagePublisher)
        {
            _outboxManager = outboxManager;
            _serviceBusMessagePublisher = serviceBusMessagePublisher;
        }

        public async Task Handle(TimeHasPassed notification, CancellationToken cancellationToken)
        {
            OutboxMessage? message;
            while ((message = _outboxManager.GetNext(OutboxMessageCategory.IntegrationEvent)) != null)
            {
                var integrationEvent = Google.Protobuf.JsonParser.Default.Parse<Contracts.IntegrationEvents.ConsumerMovedIn>(message.Data);
                await _serviceBusMessagePublisher.PublishAsync(integrationEvent).ConfigureAwait(false);
            }
        }
    }
}
