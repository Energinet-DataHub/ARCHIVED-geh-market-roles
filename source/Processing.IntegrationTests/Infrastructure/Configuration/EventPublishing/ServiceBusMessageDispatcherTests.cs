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
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus;
using Processing.IntegrationTests.Fixtures;
using Processing.IntegrationTests.TestDoubles;
using Xunit;

namespace Processing.IntegrationTests.Infrastructure.Configuration.EventPublishing
{
    public class ServiceBusMessageDispatcherTests : TestBase, IAsyncLifetime
    {
        private readonly ServiceBusMessageDispatcher _serviceBusMessageDispatcher;
        private readonly ServiceBusSenderFactorySpy _serviceBusSenderFactory;
        private readonly IntegrationEventMapper _integrationEventMapper;

        public ServiceBusMessageDispatcherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _serviceBusMessageDispatcher = GetService<ServiceBusMessageDispatcher>();
            _serviceBusSenderFactory = (ServiceBusSenderFactorySpy)GetService<IServiceBusSenderFactory>();
            _integrationEventMapper = GetService<IntegrationEventMapper>();
        }

        [Fact]
        public async Task Message_is_dispatched()
        {
            var integrationEvent = new Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn()
            {
                Id = Guid.NewGuid().ToString(),
                AccountingPointId = Guid.NewGuid().ToString(),
            };
            var eventMetadata = _integrationEventMapper.GetByType(integrationEvent.GetType());

            await _serviceBusMessageDispatcher.DispatchAsync(integrationEvent);

            _serviceBusSenderFactory.AssertPublishedMessage(eventMetadata, integrationEvent);
        }

        [Fact]
        public async Task Integration_event_id_is_required()
        {
            var integrationEvent = new Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn()
            {
                AccountingPointId = Guid.NewGuid().ToString(),
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _serviceBusMessageDispatcher.DispatchAsync(integrationEvent));
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _serviceBusSenderFactory.DisposeAsync().ConfigureAwait(false);
        }
    }
}
