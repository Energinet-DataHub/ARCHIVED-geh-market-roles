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
using Energinet.DataHub.MarketData.Domain.Customers;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes.ChangeOfSupplier.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using GreenEnergyHub.TestHelpers.Traits;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.MeteringPoints
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeSupplierTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider;

        public ChangeSupplierTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Initiate_WhenNoInterferringProcessesExists_EventIsRaised()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            meteringPoint.InitiateChangeOfSupplier(
                new ProcessId("FakeRegistrationId"),
                new GlnNumber("FakeBalanceSupplierId"),
                _systemDateTimeProvider.Now(),
                _systemDateTimeProvider);

            Assert.Contains(meteringPoint.GetDomainEvents(), e => e is ChangeOfSupplierInitiated);
        }

        [Fact]
        public void Initiate_WhenEffectuationIsCurrentDate_NotificationDateCurrentDate()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            meteringPoint.InitiateChangeOfSupplier(
                new ProcessId("FakeRegistrationId"),
                new GlnNumber("FakeBalanceSupplierId"),
                _systemDateTimeProvider.Now(),
                _systemDateTimeProvider);

            Assert.Contains(meteringPoint.GetDomainEvents(), e => e is StateChangedToAwaitingNotifySupplier);
        }

        [Fact]
        public void Initiate_WhenEffectuationIsNotCurrentDate_NotificationDateIsThreeDaysFromEffectuationDate()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            var effectuationDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(30));
            meteringPoint.InitiateChangeOfSupplier(
                new ProcessId("FakeRegistrationId"),
                new GlnNumber("FakeBalanceSupplierId"),
                effectuationDate,
                _systemDateTimeProvider);

            var expectedNotificationDate = effectuationDate.Minus(Duration.FromDays(3));

            var @event = meteringPoint.GetDomainEvents()
                .First(e => e is StateChangedToAwaitingNotifySupplier) as StateChangedToAwaitingNotifySupplier;

            Assert.Equal(expectedNotificationDate, @event!.NotifyOn);
        }

        [Fact]
        public void SetSupplierNotifiedStatus_WhenStatusIsAwaitingNotifySupplier_AwaitingEffectuationDate()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            var effectuationDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(30));
            var processId = new ProcessId("FakeProcessId");
            meteringPoint.InitiateChangeOfSupplier(
                processId,
                new GlnNumber("FakeBalanceSupplierId"),
                effectuationDate,
                _systemDateTimeProvider);

            meteringPoint.SetSupplierNotifiedStatus(processId);

            Assert.Contains(meteringPoint.GetDomainEvents(), e => e is StateChangedToAwaitingEffectuationDate);
        }

        [Fact]
        public void CompleteProcess_BeforeEffectuationDate_ThrowsException()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            var effectuationDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(30));
            var processId = new ProcessId("FakeProcessId");
            meteringPoint.InitiateChangeOfSupplier(
                processId,
                new GlnNumber("FakeBalanceSupplierId"),
                effectuationDate,
                _systemDateTimeProvider);

            meteringPoint.SetSupplierNotifiedStatus(processId);

            Assert.Throws<BusinessProcessException>(() => meteringPoint.CompleteProcess(processId, _systemDateTimeProvider));
        }

        [Fact]
        public void CompleteProcess_WhileStatusIsNotPending_ThrowsException()
        {
            var supplier = new BalanceSupplier(
                new BalanceSupplierId(CreateBalanceSupplierId().Value),
                _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)));

            var meteringPoint = new MeteringPoint(
                CreateGsrnNumber(),
                MeteringPointType.Consumption,
                supplier);

            var effectuationDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(30));
            var processId = new ProcessId("FakeProcessId");
            meteringPoint.InitiateChangeOfSupplier(
                processId,
                new GlnNumber("FakeBalanceSupplierId"),
                effectuationDate,
                _systemDateTimeProvider);

            Assert.Throws<BusinessProcessException>(() => meteringPoint.CompleteProcess(processId, _systemDateTimeProvider));
        }

        [Fact]
        public void CompleteProcess_OnEffectuationDate_SupplierIsChanged()
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

            meteringPoint.SetSupplierNotifiedStatus(processId);
            meteringPoint.CompleteProcess(processId, _systemDateTimeProvider);

            Assert.Contains(meteringPoint.GetDomainEvents(), e => e is BalanceSupplierChanged);
        }

        [Theory]
        [InlineData("exchange")]
        public void Register_WhenMeteringPointTypeIsNotEligible_IsNotPossible(string meteringPointTypeName)
        {
            var meteringPointType = CreateMeteringPointTypeFromName(meteringPointTypeName);
            var meteringPoint = CreateMeteringPoint(meteringPointType);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MeteringPointMustBeEnergySuppliableRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenProductionMeteringPointIsNotObligated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is ProductionMeteringPointMustBeObligatedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenMeteringPointIsClosedDown_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);
            meteringPoint.CloseDown();

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is CannotBeInStateOfClosedDownRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenNoEnergySupplierIsAssociated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MustHaveBalanceSupplierAssociatedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenChangeOfSupplierIsRegisteredOnSameDate_IsNotPossible()
        {
            var customerId = CreateCustomerId();
            var balanceSupplierId = CreateBalanceSupplierId();
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var registrationId = new ProcessId(Guid.NewGuid().ToString());
            meteringPoint.RegisterMoveIn(registrationId, customerId, balanceSupplierId, GetFakeEffectuationDate().Minus(Duration.FromDays(1)));
            meteringPoint.ActivateMoveIn(registrationId);
            meteringPoint.InitiateChangeOfSupplier(new ProcessId(Guid.NewGuid().ToString()), CreateBalanceSupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenMoveInIsAlreadyRegisteredOnSameDate_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var registrationId = new ProcessId(Guid.NewGuid().ToString());
            meteringPoint.RegisterMoveIn(registrationId, CreateCustomerId(), CreateBalanceSupplierId(), _systemDateTimeProvider.Now());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MoveInRegisteredOnSameDateIsNotAllowedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenMoveOutIsAlreadyRegisteredOnSameDate_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var registrationId = new ProcessId(Guid.NewGuid().ToString());
            meteringPoint.RegisterMoveIn(registrationId, CreateCustomerId(), CreateBalanceSupplierId(), GetFakeEffectuationDate().Minus(Duration.FromDays(1)));
            meteringPoint.ActivateMoveIn(registrationId);
            meteringPoint.RegisterMoveOut(CreateCustomerId(), _systemDateTimeProvider.Now());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MoveOutRegisteredOnSameDateIsNotAllowedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenEffectuationDateIsInThePast_NotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var effectuationDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));

            var result = CanChangeSupplier(meteringPoint, effectuationDate);

            Assert.Contains(result.Rules, x => x is EffectuationDateCannotBeInThePastRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenAllRulesAreSatisfied_Success()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var customerId = CreateCustomerId();
            var balanceSupplierId = CreateBalanceSupplierId();
            var registrationId = new ProcessId(Guid.NewGuid().ToString());
            meteringPoint.RegisterMoveIn(registrationId, customerId, balanceSupplierId, GetFakeEffectuationDate().Minus(Duration.FromDays(1)));
            meteringPoint.ActivateMoveIn(registrationId);

            meteringPoint.InitiateChangeOfSupplier(new ProcessId("FakeRegistrationId"), new GlnNumber("FakeBalanceSupplierId"), _systemDateTimeProvider.Now(), _systemDateTimeProvider);

            Assert.Contains(meteringPoint.DomainEvents!, e => e is ChangeOfSupplierInitiated);
        }

        private static CustomerId CreateCustomerId()
        {
            return new CustomerId("1");
        }

        private static GlnNumber CreateBalanceSupplierId()
        {
            return new GlnNumber("FakeBalanceSupplierId");
        }

        private static MeteringPoint CreateMeteringPoint(MeteringPointType meteringPointType)
        {
            var meteringPointId = CreateGsrnNumber();
            return new MeteringPoint(meteringPointId, meteringPointType);
        }

        private static GsrnNumber CreateGsrnNumber()
        {
            return GsrnNumber.Create("571234567891234568");
        }

        private static MeteringPointType CreateMeteringPointTypeFromName(string meteringPointTypeName)
        {
            return MeteringPointType.FromName<MeteringPointType>(meteringPointTypeName);
        }

        private static Instant GetFakeEffectuationDate()
        {
            return Instant.FromUtc(2000, 1, 1, 0, 0);
        }

        private BusinessRulesValidationResult CanChangeSupplier(MeteringPoint meteringPoint)
        {
            return meteringPoint.CanChangeSupplier(CreateBalanceSupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);
        }

        private BusinessRulesValidationResult CanChangeSupplier(MeteringPoint meteringPoint, Instant effectuationDate)
        {
            return meteringPoint.CanChangeSupplier(CreateBalanceSupplierId(), effectuationDate, _systemDateTimeProvider);
        }
    }
}
