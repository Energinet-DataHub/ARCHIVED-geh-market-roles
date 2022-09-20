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
using Google.Protobuf;
using Processing.Infrastructure;
using Processing.Infrastructure.Configuration;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.IntegrationTests.Application;
using Processing.IntegrationTests.Fixtures;
using Xunit;

namespace Processing.IntegrationTests.Infrastructure.Configuration.EventPublishing
{
    public class IntegrationEventRegistrationTests : TestBase
    {
        private readonly IntegrationEventMapper _mapper;

        public IntegrationEventRegistrationTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _mapper = GetService<IntegrationEventMapper>();
        }

        [Fact]
        public void Ensure_all_integration_events_are_registered()
        {
            var allIntegrationEvents =
                ApplicationAssemblies.Contracts.GetTypes()
                    .Where(x => x.GetInterfaces().Contains(typeof(IMessage)))
                    .ToList();

            Assert.True(allIntegrationEvents.TrueForAll(IsRegistered));
        }

        private bool IsRegistered(Type integrationEventType)
        {
            if (integrationEventType == null) throw new ArgumentNullException(nameof(integrationEventType));
            try
            {
                _mapper.GetByType(integrationEventType);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
