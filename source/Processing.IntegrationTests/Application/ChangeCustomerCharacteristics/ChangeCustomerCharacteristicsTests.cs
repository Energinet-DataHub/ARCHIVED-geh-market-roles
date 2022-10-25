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

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Contracts.BusinessRequests.ChangeCustomerCharacteristics;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Processing.Infrastructure.RequestAdapters;
using Processing.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using Response = Contracts.BusinessRequests.MoveIn.Response;

namespace Processing.IntegrationTests.Application.ChangeCustomerCharacteristics
{
    [IntegrationTest]
    public class ChangeCustomerCharacteristicsTests : TestBase
    {
        public ChangeCustomerCharacteristicsTests([NotNull] DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Request_succeeds()
        {
            var requestAdapter = GetService<JsonChangeCustomerCharacteristicsAdapter>();

            var request = new Request(
                SampleData.GsrnNumber,
                SampleData.MoveInDate.ToString(),
                new Customer(SampleData.CustomerNumber, SampleData.ConsumerName));

            var response = await requestAdapter.ReceiveAsync(SerializeToStream(request));

            var responseBody = await System.Text.Json.JsonSerializer.DeserializeAsync<Response>(response.Content);
            Assert.NotNull(responseBody);
            Assert.NotNull(responseBody?.ProcessId);
        }

        private static MemoryStream SerializeToStream(object request)
        {
            var stream = new MemoryStream();
            using var streamWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, request);
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
