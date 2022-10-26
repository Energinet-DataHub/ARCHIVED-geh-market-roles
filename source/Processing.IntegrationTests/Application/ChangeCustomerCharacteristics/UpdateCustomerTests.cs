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
using JetBrains.Annotations;
using NodaTime;
using Processing.Application.ChangeCustomerCharacteristics;
using Processing.Application.Common;
using Processing.Domain.Customers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.IntegrationTests.Fixtures;
using Xunit;
using Customer = Processing.Application.ChangeCustomerCharacteristics.Customer;

namespace Processing.IntegrationTests.Application.ChangeCustomerCharacteristics
{
    public class UpdateCustomerTests : TestBase, IAsyncLifetime
    {
        private AccountingPoint? _accountingPoint;

        public UpdateCustomerTests([NotNull] DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        public Task InitializeAsync()
        {
            _accountingPoint = AccountingPoint.CreateConsumption(AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber));
            GetService<IAccountingPointRepository>().Add(_accountingPoint);
            var energySupplier = new EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
            GetService<IEnergySupplierRepository>().Add(energySupplier);
            _accountingPoint.RegisterMoveIn(
                Domain.Customers.Customer.Create(CustomerNumber.Create("2605199134"), "Initial Test Name"),
                energySupplier.EnergySupplierId,
                SystemDateTimeProvider.Now(),
                BusinessProcessId.Create(SampleData.ProcessId),
                SystemDateTimeProvider.Now());
            return GetService<IUnitOfWork>().CommitAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
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
