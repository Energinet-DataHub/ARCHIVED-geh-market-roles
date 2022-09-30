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

namespace Processing.Tests.Domain.MeteringPoints.MoveIn
{
    [UnitTest]
    public class EffectuateTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider = new SystemDateTimeProviderStub();
        private readonly AccountingPoint _accountingPoint;
        private readonly ConsumerId _consumerId;
        private readonly EnergySupplierId _energySupplierId;
        private readonly BusinessProcessId _businessProcessId;

        public EffectuateTests()
        {
            _systemDateTimeProvider.SetNow(Instant.FromUtc(2020, 1, 1, 0, 0));
            _accountingPoint = AccountingPoint.CreateConsumption(AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber));
            _consumerId = new ConsumerId(Guid.NewGuid());
            _energySupplierId = new EnergySupplierId(Guid.NewGuid());
            _businessProcessId = BusinessProcessId.New();
        }

        [Fact]
        public void Effectuate_WhenAheadOfEffectiveDate_IsNotPossible()
        {
            var moveInDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(1));
            _accountingPoint.AcceptConsumerMoveIn(_consumerId, _energySupplierId, moveInDate, _businessProcessId);

            Assert.Throws<BusinessProcessException>(() =>
                _accountingPoint.EffectuateConsumerMoveIn(_businessProcessId, _systemDateTimeProvider.Now()));
        }

        [Fact]
        public void Effectuate_WhenProcessIdDoesNotExists_IsNotPossible()
        {
            var nonExistingProcessId = BusinessProcessId.New();

            Assert.Throws<BusinessProcessException>(() =>
                _accountingPoint.EffectuateConsumerMoveIn(nonExistingProcessId, _systemDateTimeProvider.Now()));
        }

        [Fact]
        public void Effectuate_WhenEffectiveDateIsDue_IsSuccessful()
        {
            GivenMoveInHasBeenAccepted(_systemDateTimeProvider.Now());

            _accountingPoint.EffectuateConsumerMoveIn(_businessProcessId, _systemDateTimeProvider.Now());

            Assert.Contains(_accountingPoint.DomainEvents, @event => @event is EnergySupplierChanged);
            Assert.Contains(_accountingPoint.DomainEvents, @event => @event is ConsumerMovedIn);

            var consumerMovedIn = _accountingPoint.DomainEvents.FirstOrDefault(de => de is ConsumerMovedIn) as ConsumerMovedIn;

            if (consumerMovedIn != null) Assert.NotNull(consumerMovedIn.MoveInDate);
        }

        private void GivenMoveInHasBeenAccepted(Instant moveInDate)
        {
            _accountingPoint.AcceptConsumerMoveIn(_consumerId, _energySupplierId, moveInDate, _businessProcessId);
        }
    }
}
