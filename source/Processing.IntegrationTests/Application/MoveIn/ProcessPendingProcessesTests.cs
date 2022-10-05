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
using MediatR;
using Processing.Application.Common.TimeEvents;
using Processing.Domain.Customers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;
using Processing.IntegrationTests.Factories;
using Processing.IntegrationTests.Fixtures;
using Processing.IntegrationTests.TestDoubles;
using Xunit;
using EffectuateConsumerMoveIn = Processing.Application.MoveIn.Processing.EffectuateConsumerMoveIn;

namespace Processing.IntegrationTests.Application.MoveIn
{
    public class ProcessPendingProcessesTests : TestBase
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;

        public ProcessPendingProcessesTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _systemDateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        }

        [Fact]
        public async Task Pending_processes_are_processed()
        {
            RegisterPendingMoveIn();

            await SimulateThatADayHasPassed();

            var command = await GetEnqueuedCommandAsync<EffectuateConsumerMoveIn>().ConfigureAwait(false);
            Assert.NotNull(command);
        }

        private async Task SimulateThatADayHasPassed()
        {
            _systemDateTimeProvider.SetCurrentTimeToMidnight();
            await GetService<IMediator>().Publish(new DayHasPassed(_systemDateTimeProvider.Now()));
        }

        private BusinessProcessId RegisterPendingMoveIn()
        {
            var supplier = CreateEnergySupplier();
            var accountingPoint = CreateAccountingPoint();
            var businessProcessId = BusinessProcessId.New();
            var customer =
                Customer.Create(CustomerNumber.Create(SampleData.CustomerNumber), SampleData.ConsumerName);
            accountingPoint.RegisterMoveIn(customer, supplier.EnergySupplierId, EffectiveDateFactory.InstantAsOfToday(), businessProcessId, _systemDateTimeProvider.Now());
            SaveChanges();
            return businessProcessId;
        }
    }
}
