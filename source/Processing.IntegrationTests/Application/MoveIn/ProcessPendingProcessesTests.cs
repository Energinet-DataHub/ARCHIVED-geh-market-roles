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
using Energinet.DataHub.MarketRoles.Contracts;
using MediatR;
using Processing.Application.Common.TimeEvents;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;
using Xunit;
using EffectuateConsumerMoveIn = Processing.Application.MoveIn.Processing.EffectuateConsumerMoveIn;

namespace Processing.IntegrationTests.Application.MoveIn
{
    public class ProcessPendingProcessesTests : TestHost
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
            var supplier = CreateEnergySupplier();
            var consumer = CreateConsumer();
            var accountingPoint = CreateAccountingPoint();
            var businessProcessId = BusinessProcessId.New();
            accountingPoint.AcceptConsumerMoveIn(consumer.ConsumerId, supplier.EnergySupplierId, EffectiveDateFactory.InstantAsOfToday(), businessProcessId);
            SaveChanges();

            _systemDateTimeProvider.SetCurrentTimeToMidnight();
            var dayHasPassed = new DayHasPassed(_systemDateTimeProvider.Now());
            await GetService<IMediator>().Publish(dayHasPassed);

            var command = await GetEnqueuedCommandAsync<EffectuateConsumerMoveIn>(businessProcessId).ConfigureAwait(false);

            Assert.NotNull(command);
        }
    }
}
