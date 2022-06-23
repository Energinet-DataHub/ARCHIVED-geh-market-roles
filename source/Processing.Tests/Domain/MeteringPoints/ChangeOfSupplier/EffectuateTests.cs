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
using System.Linq;
using NodaTime;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Xunit;
using Xunit.Categories;

namespace Processing.Tests.Domain.MeteringPoints.ChangeOfSupplier
{
    [UnitTest]
    public class EffectuateTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;

        public EffectuateTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Effectuate_WhenBeforeOfEffectiveDate_IsNotPossible()
        {
            var accountingPoint = CreateTestObject();

            var businessProcessId = BusinessProcessId.New();
            var supplyStartDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(5));
            accountingPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), supplyStartDate, _systemDateTimeProvider, businessProcessId);

            Assert.Throws<BusinessProcessException>(() => accountingPoint.EffectuateChangeOfSupplier(businessProcessId, _systemDateTimeProvider));
        }

        [Fact]
        public void Effectuate_WhenCurrentDateIsEffectiveDate_IsSuccess()
        {
            var accountingPoint = CreateTestObject();

            var supplyStartDate = _systemDateTimeProvider.Now();
            var businessProcessId = BusinessProcessId.New();
            accountingPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), supplyStartDate, _systemDateTimeProvider, businessProcessId);
            accountingPoint.EffectuateChangeOfSupplier(businessProcessId, _systemDateTimeProvider);

            var @event =
                accountingPoint.DomainEvents.FirstOrDefault(e => e is EnergySupplierChanged) as EnergySupplierChanged;

            Assert.NotNull(@event);
        }

        private static EnergySupplierId CreateEnergySupplierId()
        {
            return new EnergySupplierId(Guid.NewGuid());
        }

        private static ConsumerId CreateConsumerId()
        {
            return new ConsumerId(Guid.NewGuid());
        }

        private AccountingPoint CreateTestObject()
        {
            var accountingPoint = AccountingPoint.CreateConsumption(AccountingPointId.New(), GsrnNumber.Create("571234567891234568"));
            var businessProcessId = BusinessProcessId.New();
            accountingPoint.AcceptConsumerMoveIn(CreateConsumerId(), CreateEnergySupplierId(), _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)), businessProcessId);
            accountingPoint.EffectuateConsumerMoveIn(businessProcessId, _systemDateTimeProvider.Now());
            return accountingPoint;
        }
    }
}
