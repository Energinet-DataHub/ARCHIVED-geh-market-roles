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

using Processing.Infrastructure.Configuration.EventPublishing;
using Xunit;

namespace Processing.Tests.Infrastructure.Configuration.EventPublishing
{
    public class IntegrationEventMapperTests
    {
        [Fact]
        public void Add_new()
        {
            var mapper = new IntegrationEventMapper();

            mapper.Add(nameof(Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn), typeof(Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn), 1, "consumer-moved-in");

            var eventMetadata = mapper.GetByName(nameof(Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn));
            Assert.NotNull(eventMetadata);
        }

        [Fact]
        public void Can_get_by_event_type()
        {
            var mapper = new IntegrationEventMapper();

            mapper.Add(nameof(Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn), typeof(Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn), 1, "consumer-moved-in");

            var eventMetadata = mapper.GetByType(typeof(Energinet.DataHub.EnergySupplying.IntegrationEvents.ConsumerMovedIn));
            Assert.NotNull(eventMetadata);
        }
    }
}
