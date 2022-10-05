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
using Processing.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Processing.Domain.SeedWork;
using Xunit;
using Xunit.Categories;

namespace Processing.Tests.Domain.MeteringPoints.ChangeOfSupplier
{
    [UnitTest]
    public class AcceptTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;

        public AcceptTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Accept_WhenProductionMeteringPointIsNotObligated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production, false);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, error => error is ProductionMeteringPointMustBeObligatedRuleError);
        }

        [Fact]
        public void Accept_WhenMeteringPointIsClosedDown_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);
            meteringPoint.CloseDown();

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, error => error is CannotBeInStateOfClosedDownRuleError);
        }

        [Fact]
        public void Accept_WhenNoEnergySupplierIsAssociated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, error => error is MustHaveEnergySupplierAssociatedRuleError);
        }

        [Fact]
        public void Accept_WhenChangeOfSupplierIsRegisteredOnSameDate_IsNotPossible()
        {
            var energySupplierId = CreateSupplierId();
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var moveInDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));
            var businessProcessId = BusinessProcessId.New();

            meteringPoint.RegisterMoveIn(CreateCustomer(), energySupplierId, moveInDate, businessProcessId, _systemDateTimeProvider.Now());
            meteringPoint.EffectuateConsumerMoveIn(businessProcessId, _systemDateTimeProvider.Now());
            meteringPoint.AcceptChangeOfSupplier(CreateSupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider, BusinessProcessId.New());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, error => error is ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleError);
        }

        [Fact]
        public void Accept_WhenMoveInIsAlreadyRegisteredOnSameDate_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var moveInDate = _systemDateTimeProvider.Now();
            var businessProcessId = BusinessProcessId.New();
            meteringPoint.RegisterMoveIn(CreateCustomer(), CreateSupplierId(), moveInDate,  businessProcessId, _systemDateTimeProvider.Now());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, error => error is MoveInRegisteredOnSameDateIsNotAllowedRuleError);
        }

        // TODO: Ignore Move related rules until implementation is in scope
        // [Fact]
        // public void Register_WhenMoveOutIsAlreadyRegisteredOnSameDate_IsNotPossible()
        // {
        //     var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
        //
        //     var moveInDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));
        //     meteringPoint.RegisterMoveIn(CreateConsumerId(), CreateSupplierId(), moveInDate, CreateProcessId());
        //     meteringPoint.RegisterMoveOut(CreateCustomerId(), _systemDateTimeProvider.Now());
        //
        //     var result = CanChangeSupplier(meteringPoint);
        //
        //     Assert.Contains(result.Errors, x => x.Rule == typeof(MoveOutRegisteredOnSameDateIsNotAllowedRule));
        // }
        [Fact]
        public void Accept_WhenEffectuationDateIsInThePast_NotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var effectuationDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));

            var result = CanChangeSupplier(meteringPoint, effectuationDate);

            Assert.Contains(result.Errors, error => error is EffectiveDateCannotBeInThePastRuleError);
        }

        [Fact]
        public void Accept_WhenAllRulesAreSatisfied_Success()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var energySupplierId = CreateSupplierId();
            var moveInDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));
            var businessProcessId = BusinessProcessId.New();
            meteringPoint.RegisterMoveIn(CreateCustomer(), energySupplierId, moveInDate, businessProcessId, _systemDateTimeProvider.Now());
            meteringPoint.EffectuateConsumerMoveIn(businessProcessId, _systemDateTimeProvider.Now());

            meteringPoint.AcceptChangeOfSupplier(CreateSupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider, BusinessProcessId.New());

            Assert.Contains(meteringPoint.DomainEvents!, e => e is EnergySupplierChangeRegistered);
        }

        private static Customer CreateCustomer()
        {
            return Customer.Create(CustomerNumber.Create(SampleData.ConsumerSocialSecurityNumber), SampleData.ConsumerName);
        }

        private static EnergySupplierId CreateSupplierId()
        {
            return new EnergySupplierId(Guid.NewGuid());
        }

        private static AccountingPoint CreateMeteringPoint(MeteringPointType meteringPointType, bool isObligated = true)
        {
            var gsrnNumber = CreateGsrnNumber();

            if (meteringPointType == MeteringPointType.Consumption)
            {
                return AccountingPoint.CreateConsumption(AccountingPointId.New(), gsrnNumber);
            }

            if (meteringPointType == MeteringPointType.Production)
            {
                return AccountingPoint.CreateProduction(AccountingPointId.New(), gsrnNumber, isObligated);
            }

            throw new InvalidOperationException();
        }

        private static GsrnNumber CreateGsrnNumber()
        {
            return GsrnNumber.Create("571234567891234568");
        }

        private static MeteringPointType CreateMeteringPointTypeFromName(string meteringPointTypeName)
        {
            return MeteringPointType.FromName<MeteringPointType>(meteringPointTypeName);
        }

        private BusinessRulesValidationResult CanChangeSupplier(AccountingPoint accountingPoint)
        {
            return accountingPoint.ChangeSupplierAcceptable(CreateSupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);
        }

        private BusinessRulesValidationResult CanChangeSupplier(AccountingPoint accountingPoint, Instant effectuationDate)
        {
            return accountingPoint.ChangeSupplierAcceptable(CreateSupplierId(), effectuationDate, _systemDateTimeProvider);
        }
    }
}
