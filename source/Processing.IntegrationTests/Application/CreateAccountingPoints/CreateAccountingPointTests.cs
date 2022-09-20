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
using Dapper;
using Processing.Application.AccountingPoints;
using Processing.Application.Common;
using Processing.Domain.MeteringPoints;
using Processing.IntegrationTests.Fixtures;
using Xunit;

namespace Processing.IntegrationTests.Application.CreateAccountingPoints
{
    public class CreateAccountingPointTests : TestBase
    {
        public CreateAccountingPointTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Can_create_accounting_point()
        {
            var command = new CreateAccountingPoint(
                Guid.NewGuid().ToString(),
                SampleData.GsrnNumber,
                MeteringPointType.Consumption.Name);

            await InvokeCommandAsync(command).ConfigureAwait(false);

            var checkStatement =
                $"SELECT COUNT(1) FROM [dbo].[AccountingPoints] WHERE GsrnNumber = @GsrnNumber AND Type = @Type";
            var found = GetService<IDbConnectionFactory>().GetOpenConnection().ExecuteScalar<bool>(
                checkStatement,
                new
                {
                    GsrnNumber = SampleData.GsrnNumber,
                    Type = MeteringPointType.Consumption.Id,
                });
        }
    }
}
