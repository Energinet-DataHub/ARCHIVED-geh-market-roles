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
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Processing.Application.Common;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus;
using Processing.Infrastructure.Configuration.Outbox;
using Processing.IntegrationTests.Application;
using Processing.IntegrationTests.Fixtures;
using Processing.IntegrationTests.TestDoubles;
using Xunit;

namespace Processing.IntegrationTests.Infrastructure.Configuration.EventPublishing
{
    public class EventDispatcherTests : TestBase
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly EventDispatcher _eventDispatcher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly OutboxProvider _outboxProvider;

        public EventDispatcherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _eventPublisher = GetService<IEventPublisher>();
            _eventDispatcher = GetService<EventDispatcher>();
            _unitOfWork = GetService<IUnitOfWork>();
            _outboxProvider = GetService<OutboxProvider>();
        }

        [Fact]
        public async Task Event_is_dispatched()
        {
            await PublishEvent();

            await _eventDispatcher.DispatchAsync().ConfigureAwait(false);

            Assert.Null(_outboxProvider.GetNext());
        }

        private async Task PublishEvent()
        {
            var integrationEvent = new ConsumerMovedIn()
            {
                Id = Guid.NewGuid().ToString(),
                AccountingPointId = Guid.NewGuid().ToString(),
            };
            var eventMetadata = GetService<IntegrationEventMapper>().GetByType(integrationEvent.GetType())!;
            await using var serviceBusSenderAdapter = new ServiceBusSenderSpy(eventMetadata.TopicName);
            await _eventPublisher.PublishAsync(integrationEvent).ConfigureAwait(false);
            await _unitOfWork.CommitAsync().ConfigureAwait(false);
        }
    }
}
