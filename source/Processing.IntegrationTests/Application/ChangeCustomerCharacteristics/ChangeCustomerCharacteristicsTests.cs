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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Contracts.BusinessRequests.ChangeCustomerCharacteristics;
using Dapper;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Processing.Application.Common;
using Processing.Infrastructure.RequestAdapters;
using Processing.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using Response = Contracts.BusinessRequests.MoveIn.Response;

namespace Processing.IntegrationTests.Application.ChangeCustomerCharacteristics
{
    [IntegrationTest]
    public class ChangeCustomerCharacteristicsTests : TestBase, IAsyncLifetime
    {
        public ChangeCustomerCharacteristicsTests([NotNull] DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        public async Task InitializeAsync()
        {
            await CreateMoveInTransaction().ConfigureAwait(false);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Request_succeeds()
        {
            var requestAdapter = GetService<JsonChangeCustomerCharacteristicsAdapter>();

            var request = new Request(
                Processing.IntegrationTests.Application.SampleData.GsrnNumber,
                Processing.IntegrationTests.Application.SampleData.MoveInDate.ToString(),
                new Customer(Processing.IntegrationTests.Application.SampleData.CustomerNumber, Processing.IntegrationTests.Application.SampleData.ConsumerName),
                Guid.NewGuid().ToString());

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

        private async Task CreateMoveInTransaction()
        {
            var sql = @$"INSERT INTO [b2b].[MoveInTransactions]
                            ([TransactionId],
                             [ProcessId],
                             [MarketEvaluationPointId],
                             [EffectiveDate],
                             [CurrentEnergySupplierId],
                             [State],
                             [StartedByMessageId],
                             [NewEnergySupplierId],
                             [ConsumerId],
                             [ConsumerName],
                             [ConsumerIdType],
                             [CurrentEnergySupplierNotificationState],
                             [MeteringPointMasterDataState],
                             [CustomerMasterDataState],
                             [BusinessProcessState],
                             [GridOperatorNotificationState],
                             [GridOperator_MessageDeliveryState_CustomerMasterData],
                             [CustomerMasterData])
                            VALUES ('{SampleData.TransactionId}',
                              '{SampleData.ProcessId}',
                              '{SampleData.MarketEvaluationPointId}',
                              '{SampleData.EffectiveDate}',
                              '{SampleData.CurrentEnergySupplierId}',
                              '{SampleData.State}',
                              '{SampleData.StartedByMessageId}',
                              '{SampleData.NewEnergySupplierId}',
                              '{SampleData.ConsumerId}',
                              '{SampleData.ConsumerName}',
                              '{SampleData.ConsumerIdType}',
                              '{SampleData.CurrentEnergySupplierNotificationState}',
                              '{SampleData.MeteringPointMasterDataState}',
                              '{SampleData.CustomerMasterDataState}',
                              '{SampleData.BusinessProcessState}',
                              '{SampleData.GridOperatorNotificationState}',
                              '{SampleData.GridOperatorMessageDeliveryStateCustomerMasterData}',
                              '{SampleData.CustomerMasterData}')";
            await GetService<IDbConnectionFactory>().GetOpenConnection().ExecuteAsync(
                sql,
                new { }).ConfigureAwait(false);
        }
    }
}
