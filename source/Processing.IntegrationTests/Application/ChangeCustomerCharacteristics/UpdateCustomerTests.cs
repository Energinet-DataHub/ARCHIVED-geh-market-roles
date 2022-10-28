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

using System.Linq;
using System.Threading.Tasks;
using Dapper;
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

        public UpdateCustomerTests(DatabaseFixture databaseFixture)
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

            await AssertCustomerMasterData().ConfigureAwait(false);
        }

        [Fact]
        public async Task Accounting_point_must_be_available()
        {
            var request = CreateRequest()
                with
                {
                    AccountingPointNumber = "571234567891234551",
                };

            var result = await SendRequestAsync(request).ConfigureAwait(false);
            var error = result.ValidationErrors.FirstOrDefault();

            Assert.NotNull(error);
            Assert.Equal("UnknownAccountingPoint", error?.Code);
        }

        private static ChangeCustomerMasterDataRequest CreateRequest()
        {
            return new ChangeCustomerMasterDataRequest(
                SampleData.GsrnNumber,
                SampleData.ProcessId,
                new Customer(SampleData.CustomerNumber, SampleData.ConsumerName),
                new Customer(SampleData.SecondConsumerNumber, SampleData.SecondConsumerName));
        }

        private async Task AssertCustomerMasterData()
        {
            var sql = $"SELECT CustomerName AS {nameof(DataModel.CustomerName)}, " +
                      $"CustomerNumber AS {nameof(DataModel.CustomerNumber)}, " +
                      $"SecondCustomerName AS {nameof(DataModel.SecondCustomerName)}, " +
                      $"SecondCustomerNumber AS {nameof(DataModel.SecondCustomerNumber)} " +
                      $"FROM [dbo].ConsumerRegistrations " +
                      $"WHERE BusinessProcessId = @ProcessId";
            var customerDataConsumerRegistration = await GetService<IDbConnectionFactory>().GetOpenConnection().QuerySingleOrDefaultAsync<DataModel>(
                sql,
                new { ProcessId = SampleData.ProcessId }).ConfigureAwait(false);

            Assert.Equal(SampleData.ConsumerName, customerDataConsumerRegistration.CustomerName);
            Assert.Equal(SampleData.CustomerNumber, customerDataConsumerRegistration.CustomerNumber);
            Assert.Equal(SampleData.SecondConsumerName, customerDataConsumerRegistration.SecondCustomerName);
            Assert.Equal(SampleData.SecondConsumerNumber, customerDataConsumerRegistration.SecondCustomerNumber);
        }
    }

    public record DataModel(string CustomerName, string CustomerNumber, string SecondCustomerName, string SecondCustomerNumber);
}
