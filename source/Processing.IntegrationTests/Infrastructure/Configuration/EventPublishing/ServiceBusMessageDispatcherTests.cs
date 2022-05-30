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
using Processing.IntegrationTests.Application;
using Processing.IntegrationTests.TestDoubles;
using Xunit;

namespace Processing.IntegrationTests.Infrastructure.Configuration.EventPublishing
{
    public class ServiceBusMessageDispatcherTests : TestHost
    {
        private readonly ServiceBusMessageDispatcher _serviceBusMessageDispatcher;
        private readonly IServiceBusSenderFactory _serviceBusSenderFactory;
        private readonly IntegrationEventMapper _integrationEventMapper;

        public ServiceBusMessageDispatcherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _serviceBusMessageDispatcher = GetService<ServiceBusMessageDispatcher>();
            _serviceBusSenderFactory = GetService<IServiceBusSenderFactory>();
            _integrationEventMapper = GetService<IntegrationEventMapper>();
        }

        [Fact]
        public async Task Message_is_dispatched()
        {
            var integrationEvent = new Contracts.IntegrationEvents.ConsumerMovedIn()
            {
                AccountingPointId = Guid.NewGuid().ToString(),
            };
            var eventMetadata = _integrationEventMapper.GetByType(integrationEvent.GetType());
            var senderSpy = new ServiceBusSenderSpy(eventMetadata!.TopicName);
            AddSenderSpy(senderSpy);

            await _serviceBusMessageDispatcher.DispatchAsync(integrationEvent);

            Assert.NotNull(senderSpy.Message);
            Assert.Equal("application/octet-stream;charset=utf-8", senderSpy.Message!.ContentType);
            Assert.NotNull(senderSpy.Message!.Body);
            Assert.NotNull(senderSpy.Message!.ApplicationProperties["OperationTimestamp"]);
            Assert.Equal(eventMetadata!.Version, senderSpy.Message!.ApplicationProperties["MessageVersion"]);
            Assert.Equal(eventMetadata!.EventName, senderSpy.Message!.ApplicationProperties["MessageType"]);
            Assert.NotNull(senderSpy.Message!.ApplicationProperties["EventIdentification"]);
            Assert.NotNull(senderSpy.Message!.ApplicationProperties["OperationCorrelationId"]);
        }

        private void AddSenderSpy(ServiceBusSenderSpy senderSpy)
        {
            (_serviceBusSenderFactory as ServiceBusSenderFactoryStub)!.AddSenderSpy(senderSpy);
        }
    }
}
