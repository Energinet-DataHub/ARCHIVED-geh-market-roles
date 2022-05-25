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
using System.Linq;
using System.Threading.Tasks;
using Contracts.IntegrationEvents;
using Processing.Application.Common;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.IntegrationTests.Application;
using Processing.IntegrationTests.TestDoubles;
using Xunit;

namespace Processing.IntegrationTests.Infrastructure.Configuration.EventPublishing
{
    public class EventDispatcherTests : TestHost
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly EventDispatcher _eventDispatcher;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IUnitOfWork _unitOfWork;

        public EventDispatcherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _eventPublisher = GetService<IEventPublisher>();
            _eventDispatcher = GetService<EventDispatcher>();
            _messageDispatcher = GetService<IMessageDispatcher>();
            _unitOfWork = GetService<IUnitOfWork>();
        }

        [Fact]
        public async Task Event_is_dispatched()
        {
            var integrationEvent = new ConsumerMovedIn() { AccountingPointId = Guid.NewGuid().ToString(), };
            await _eventPublisher.PublishAsync(integrationEvent).ConfigureAwait(false);
            await _unitOfWork.CommitAsync().ConfigureAwait(false);

            await _eventDispatcher.DispatchAsync().ConfigureAwait(false);

            var publishedMessages = (_messageDispatcher as MessageDispatcherStub)!.PublishedMessages;
            Assert.Equal(integrationEvent, publishedMessages.First());
        }
    }
}
