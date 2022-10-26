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

using System.Threading.Tasks;
using JetBrains.Annotations;
using Processing.Application.ChangeCustomerCharacteristics;
using Processing.IntegrationTests.Fixtures;
using Xunit;

namespace Processing.IntegrationTests.Application.ChangeCustomerCharacteristics
{
    public class UpdateCustomerTests : TestBase
    {
        public UpdateCustomerTests([NotNull] DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Customer_master_data_must_be_updated()
        {
            var request = CreateRequest();

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        private static ChangeCustomerCharacteristicsRequest CreateRequest()
        {
            return new ChangeCustomerCharacteristicsRequest(
                SampleData.ProcessId,
                new Customer(SampleData.ConsumerName, SampleData.CustomerNumber));
        }
    }
}
