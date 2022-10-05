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
using NodaTime;
using Processing.Domain.Customers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Xunit;
using Xunit.Categories;

namespace Processing.Tests.Domain.MeteringPoints.ChangeOfSupplier
{
    [UnitTest]
    public class CancellationTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;

        public CancellationTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Cancel_WhenProcessIsPending_Success()
        {
            var (meteringPoint, _) = CreateWithActiveMoveIn();
            var businessProcessId = BusinessProcessId.New();
            meteringPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), _systemDateTimeProvider.Now().Plus(Duration.FromDays(5)), _systemDateTimeProvider, businessProcessId);

            meteringPoint.CancelChangeOfSupplier(businessProcessId);

            Assert.Contains(meteringPoint.DomainEvents !, e => e is ChangeOfSupplierCancelled);
        }

        [Fact]
        public void Cancel_WhenIsNotPending_IsNotPossible()
        {
            var (meteringPoint, _) = CreateWithActiveMoveIn();
            var supplyStartDate = _systemDateTimeProvider.Now();
            var businessProcessId = BusinessProcessId.New();
            meteringPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), supplyStartDate, _systemDateTimeProvider, businessProcessId);
            meteringPoint.EffectuateChangeOfSupplier(businessProcessId, _systemDateTimeProvider);

            Assert.Throws<BusinessProcessException>(() => meteringPoint.CancelChangeOfSupplier(businessProcessId));
        }

        private static EnergySupplierId CreateEnergySupplierId()
        {
            return new EnergySupplierId(Guid.NewGuid());
        }

        private static Customer CreateCustomer()
        {
            return Customer.Create(CustomerNumber.Create(SampleData.ConsumerSocialSecurityNumber), SampleData.ConsumerName);
        }

        private (AccountingPoint AccountingPoint, BusinessProcessId ProcessId) CreateWithActiveMoveIn()
        {
            var accountingPoint = AccountingPoint.CreateConsumption(AccountingPointId.New(), GsrnNumber.Create("571234567891234568"));
            var businessProcessId = BusinessProcessId.New();
            accountingPoint.RegisterMoveIn(CreateCustomer(), CreateEnergySupplierId(), _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)), businessProcessId, _systemDateTimeProvider.Now());
            accountingPoint.EffectuateConsumerMoveIn(businessProcessId, _systemDateTimeProvider.Now());
            return (accountingPoint, businessProcessId);
        }
    }
}
