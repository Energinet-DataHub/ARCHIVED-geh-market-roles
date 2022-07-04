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
using MediatR;
using Processing.Application.AccountingPoint;
using Processing.Domain.MeteringPoints;
using Xunit;

namespace Processing.IntegrationTests.Application.CreateAccountingPoint
{
    public class CreateAccountingPointTests : TestHost
    {
        public CreateAccountingPointTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task CreateAccountingPointCommandNotCreatedWhenMeteringPointTypeIsNotConsumptionOrProductionAsync()
        {
            await SimulateIncomingMeteringPointCreatedEventWithNoneAccountingPointTypeAsync().ConfigureAwait(false);

            var command = await GetEnqueuedCommandAsync<Processing.Application.AccountingPoint.CreateAccountingPoint>();

            Assert.Null(command);
        }

        private async Task AssertAccountingPointAsync(MeteringPointCreated meteringPoint)
        {
            var accountingPoint = await GetAccountingPointAsync(meteringPoint).ConfigureAwait(false);
            Assert.Equal(meteringPoint.MeteringPointId, accountingPoint.Id.Value.ToString());
            Assert.Equal(meteringPoint.GsrnNumber, accountingPoint.GsrnNumber.Value);
        }

        private async Task<AccountingPoint> GetAccountingPointAsync(MeteringPointCreated meteringPoint)
        {
           return (await AccountingPointRepository.GetByIdAsync(AccountingPointId.Create(Guid.Parse(meteringPoint.MeteringPointId)))
               .ConfigureAwait(false))!;
        }

        private async Task<MeteringPointCreated> SimulateIncomingMeteringPointCreatedEventWithTypeConsumptionAsync()
        {
            var meteringPoint = new MeteringPointCreated(
                Guid.NewGuid().ToString(),
                SampleData.GsrnNumber,
                MeteringPointType.Consumption);

            await GetService<IMediator>().Publish(meteringPoint).ConfigureAwait(false);

            SaveChanges();

            return meteringPoint;
        }

        private async Task SimulateIncomingMeteringPointCreatedEventWithNoneAccountingPointTypeAsync()
        {
            var meteringPoint = new MeteringPointCreated(
                Guid.NewGuid().ToString(),
                SampleData.GsrnNumber,
                MeteringPointType.NoneAccountingPoint);

            await GetService<IMediator>().Publish(meteringPoint).ConfigureAwait(false);

            SaveChanges();
        }
    }
}
