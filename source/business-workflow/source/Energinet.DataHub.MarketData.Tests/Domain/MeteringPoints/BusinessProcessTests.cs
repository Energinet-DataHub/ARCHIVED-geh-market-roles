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

using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes.ChangeOfSupplier.Events;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.MeteringPoints
{
    public class BusinessProcessTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider;

        public BusinessProcessTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Cancel_WhenStatusIsNotCompleted_IsCancelled()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            var effectuationDate = _systemDateTimeProvider.Now();
            var processId = new ProcessId("FakeProcessId");
            meteringPoint.InitiateChangeOfSupplier(
                processId,
                new GlnNumber("FakeBalanceSupplierId"),
                effectuationDate,
                _systemDateTimeProvider);

            meteringPoint.CancelProcess(processId);

            Assert.Contains(meteringPoint.GetDomainEvents(), e => e is ChangeOfSupplierProcessCancelled);
        }

        private static GlnNumber CreateBalanceSupplierId()
        {
            return new GlnNumber("FakeBalanceSupplierId");
        }

        private static GsrnNumber CreateGsrnNumber()
        {
            return GsrnNumber.Create("571234567891234568");
        }
    }
}
