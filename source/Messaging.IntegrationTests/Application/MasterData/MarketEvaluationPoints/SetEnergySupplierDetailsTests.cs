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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData.MarketEvaluationPoints;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.MasterData.MarketEvaluationPoints;

public class SetEnergySupplierDetailsTests : TestBase
{
    public SetEnergySupplierDetailsTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Market_evaluation_point_is_created_if_it_does_not_exist()
    {
        var command = new SetEnergySupplier(
            marketEvaluationPointNumber: SampleData.AccountingPointNumber,
            energySupplierNumber: SampleData.EnergySupplierNumber);

        await InvokeCommandAsync(command).ConfigureAwait(false);

        var found = await GetService<IDbConnectionFactory>()
            .GetOpenConnection()
            .ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM b2b.MarketEvaluationPoints WHERE EnergySupplierNumber = @EnergySupplierNumber AND MarketEvaluationPointNumber = @MarketEvaluationPointNumber",
                new
                {
                    EnergySupplierNumber = command.EnergySupplierNumber,
                    MarketEvaluationPointNumber = command.MarketEvaluationPointNumber,
                })
            .ConfigureAwait(false);

        Assert.True(found);
    }

    [Fact]
    public async Task Energy_supplier_is_changed()
    {
        await InvokeCommandAsync(new SetEnergySupplier(
            marketEvaluationPointNumber: SampleData.AccountingPointNumber,
            energySupplierNumber: SampleData.EnergySupplierNumber)).ConfigureAwait(false);

        var command = new SetEnergySupplier(SampleData.AccountingPointNumber, SampleData.NewEnergySupplierNumber);
        await InvokeCommandAsync(command).ConfigureAwait(false);

        var found = await GetService<IDbConnectionFactory>()
            .GetOpenConnection()
            .ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM b2b.MarketEvaluationPoints WHERE EnergySupplierNumber = @EnergySupplierNumber AND MarketEvaluationPointNumber = @MarketEvaluationPointNumber",
                new
                {
                    EnergySupplierNumber = command.EnergySupplierNumber,
                    MarketEvaluationPointNumber = command.MarketEvaluationPointNumber,
                })
            .ConfigureAwait(false);

        Assert.True(found);
    }
}
