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
using Processing.Application.AccountingPoint;
using Processing.Application.AccountingPoint.GetCurrentSupplierDetails;
using Processing.Application.MoveIn;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Infrastructure.Configuration.DataAccess;
using Xunit;
using EnergySupplier = Processing.Domain.EnergySuppliers.EnergySupplier;

namespace Processing.IntegrationTests.Application.AccoutingPoints
{
    public class GetCurrentSupplierDetailsTests : TestHost
    {
        public GetCurrentSupplierDetailsTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Get_current_energy_supplier_details()
        {
            await SetupScenario().ConfigureAwait(false);

            var result = await QueryAsync(new GetCurrentSupplierDetailsQuery(SampleData.GsrnNumber)).ConfigureAwait(false);

            Assert.Equal(SampleData.GlnNumber, result.EnergySupplier?.EnergySupplierNumber);
        }

        [Fact]
        public async Task Return_error_if_accounting_point_does_not_exist()
        {
            var result = await QueryAsync(new GetCurrentSupplierDetailsQuery("Not_existing_GSRN_number")).ConfigureAwait(false);

            Assert.Null(result.EnergySupplier);
            Assert.True(result.Error.Length > 0);
        }

        [Fact]
        public async Task Return_error_if_accounting_point_does_not_have_a_supplier()
        {
            await CreateAccoutingPoint();

            var result = await QueryAsync(new GetCurrentSupplierDetailsQuery(SampleData.GsrnNumber)).ConfigureAwait(false);

            Assert.Null(result.EnergySupplier);
            Assert.True(result.Error.Length > 0);
        }

        private async Task SetupScenario()
        {
            await CreateEnergySupplier();
            await CreateAccoutingPoint();
            await MoveInConsumer();
        }

        private async Task MoveInConsumer()
        {
            await InvokeCommandAsync(new MoveInRequest(
                new Consumer("Consumer1", "1111111234", "CPR"),
                SampleData.GlnNumber,
                SampleData.GsrnNumber,
                EffectiveDateFactory.AsOfToday().ToString()));
        }

        private async Task CreateAccoutingPoint()
        {
            await InvokeCommandAsync(new CreateAccountingPoint(
                Guid.NewGuid().ToString(),
                SampleData.GsrnNumber,
                MeteringPointType.Consumption.Name,
                PhysicalState.New.Name));
        }

        private async Task CreateEnergySupplier()
        {
            GetService<IEnergySupplierRepository>().Add(new Domain.EnergySuppliers.EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber)));
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        }
    }
}
