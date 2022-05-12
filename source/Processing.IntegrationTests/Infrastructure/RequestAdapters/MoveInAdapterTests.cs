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
using Newtonsoft.Json;
using Processing.Infrastructure.RequestAdapters;
using Processing.IntegrationTests.Application;
using Xunit;

namespace Processing.IntegrationTests.Infrastructure.RequestAdapters
{
    public class MoveInAdapterTests : TestHost
    {
        private readonly MoveInAdapter _adapter;

        public MoveInAdapterTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _adapter = GetService<MoveInAdapter>();
        }

        [Fact]
        public async Task Can_handle()
        {
            var request = new MoveInRequestDto(
                "ConsumerName",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);

            var result = await _adapter.ReceiveAsync(SerializeToStream(request)).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.NotNull(result.Content);
        }

        private static MemoryStream SerializeToStream(object request)
        {
            var stream = new MemoryStream();
            using var streamWriter = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.Serialize(jsonWriter, request);
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
