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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NodaTime;
using Processing.Domain.Customers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints.Events;
using Processing.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Processing.Domain.MeteringPoints.Rules.MoveIn;
using Processing.Domain.SeedWork;

namespace Processing.Domain.MeteringPoints
{
    public sealed class AccountingPoint : AggregateRootBase
    {
        private readonly MeteringPointType _meteringPointType;
        private readonly bool _isProductionObligated;
        private readonly List<BusinessProcess> _businessProcesses = new();
        private readonly List<ConsumerRegistration> _consumerRegistrations = new();
        private readonly List<SupplierRegistration> _supplierRegistrations = new();
        private PhysicalState _physicalState;

        // constructor to satisfy EF
        public AccountingPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, PhysicalState physicalState)
        {
            Id = AccountingPointId.New();
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = physicalState;
        }

        private AccountingPoint(AccountingPointId meteringPointId, GsrnNumber gsrnNumber, MeteringPointType meteringPointType)
        {
            Id = meteringPointId;
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = PhysicalState.New;
            AddDomainEvent(new MeteringPointCreated(GsrnNumber, _meteringPointType));
        }

        private AccountingPoint(AccountingPointId meteringPointId, GsrnNumber gsrnNumber, MeteringPointType meteringPointType, bool isProductionObligated)
            : this(meteringPointId, gsrnNumber, meteringPointType)
        {
            _isProductionObligated = isProductionObligated;
        }

        public AccountingPointId Id { get; }

        public GsrnNumber GsrnNumber { get; }

        public static AccountingPoint CreateProduction(AccountingPointId meteringPointId, GsrnNumber gsrnNumber, bool isObligated)
        {
            return new AccountingPoint(meteringPointId, gsrnNumber, MeteringPointType.Production, isObligated);
        }

        public static AccountingPoint CreateConsumption(AccountingPointId meteringPointId, GsrnNumber gsrnNumber)
        {
            return new AccountingPoint(meteringPointId, gsrnNumber, MeteringPointType.Consumption);
        }

        public void SetElectricalHeating(ElectricalHeating? electricalHeating)
        {
            if (electricalHeating is not null)
            {
                AddDomainEvent(new ElectricalHeatingWasSet(Id.Value, electricalHeating.EffectiveDate.DateInUtc));
            }
        }

        public BusinessRulesValidationResult ChangeSupplierAcceptable(EnergySupplierId energySupplierId, Instant supplyStartDate, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (energySupplierId is null)
            {
                throw new ArgumentNullException(nameof(energySupplierId));
            }

            if (systemDateTimeProvider == null)
            {
                throw new ArgumentNullException(nameof(systemDateTimeProvider));
            }

            var rules = new Collection<IBusinessRule>()
            {
                new MeteringPointMustBeEnergySuppliableRule(_meteringPointType),
                new ProductionMeteringPointMustBeObligatedRule(_meteringPointType, _isProductionObligated),
                new CannotBeInStateOfClosedDownRule(_physicalState),
                new MustHaveEnergySupplierAssociatedRule(GetCurrentSupplier(systemDateTimeProvider)),
                new ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                new EffectiveDateCannotBeInThePastRule(supplyStartDate, systemDateTimeProvider.Now()),
                new CannotBeCurrentSupplierRule(energySupplierId, GetCurrentSupplier(systemDateTimeProvider)),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void AcceptChangeOfSupplier(EnergySupplierId energySupplierId, Instant supplyStartDate, ISystemDateTimeProvider systemDateTimeProvider, BusinessProcessId businessProcessId)
        {
            if (energySupplierId == null) throw new ArgumentNullException(nameof(energySupplierId));
            if (systemDateTimeProvider == null) throw new ArgumentNullException(nameof(systemDateTimeProvider));
            if (businessProcessId == null) throw new ArgumentNullException(nameof(businessProcessId));
            if (!ChangeSupplierAcceptable(energySupplierId, supplyStartDate, systemDateTimeProvider).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept change of supplier request due to violation of one or more business rules.");
            }

            var businessProcess = CreateBusinessProcess(supplyStartDate, BusinessProcessType.ChangeOfSupplier, businessProcessId);
            _businessProcesses.Add(businessProcess);
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, businessProcess.BusinessProcessId));

            AddDomainEvent(new EnergySupplierChangeRegistered(Id, GsrnNumber, businessProcess.BusinessProcessId, supplyStartDate, energySupplierId));
        }

        public void EffectuateChangeOfSupplier(BusinessProcessId processId, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (processId is null) throw new ArgumentNullException(nameof(processId));
            if (systemDateTimeProvider == null) throw new ArgumentNullException(nameof(systemDateTimeProvider));

            var businessProcess = GetBusinessProcess(processId, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Effectuate(systemDateTimeProvider);

            DiscontinueCurrentSupplier(businessProcess, systemDateTimeProvider);

            var futureSupplier = GetFutureSupplierRegistration(businessProcess);
            StartOfSupplyForFutureSupplier(businessProcess, futureSupplier);

            AddDomainEvent(new EnergySupplierChanged(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, futureSupplier.EnergySupplierId.Value, businessProcess.EffectiveDate));
        }

        public void CloseDown()
        {
            if (_physicalState != PhysicalState.ClosedDown)
            {
                _physicalState = PhysicalState.ClosedDown;
                AddDomainEvent(new MeteringPointClosedDown(GsrnNumber));
            }
        }

        public BusinessRulesValidationResult ConsumerMoveInAcceptable(Instant moveInDate, Customer customer, Instant today)
        {
            var rules = new Collection<IBusinessRule>()
            {
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), moveInDate),
                new CustomerMustBeDifferentFromCurrentCustomerRule(customer, _consumerRegistrations.AsReadOnly(), today),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void RegisterMoveIn(Customer customer, EnergySupplierId energySupplierId, Instant moveInDate, BusinessProcessId businessProcessId, Instant today)
        {
            if (energySupplierId == null) throw new ArgumentNullException(nameof(energySupplierId));
            if (businessProcessId == null) throw new ArgumentNullException(nameof(businessProcessId));
            if (!ConsumerMoveInAcceptable(moveInDate, customer, today).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept move in request due to violation of one or more business rules.");
            }

            var businessProcess = CreateBusinessProcess(moveInDate, BusinessProcessType.MoveIn, businessProcessId);
            _businessProcesses.Add(businessProcess);
            _consumerRegistrations.Add(new ConsumerRegistration(customer, businessProcess.BusinessProcessId));
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, businessProcess.BusinessProcessId));

            AddDomainEvent(new ConsumerMoveInAccepted(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, energySupplierId.Value, moveInDate));
        }

        public void EffectuateConsumerMoveIn(BusinessProcessId processId, Instant today)
        {
            if (processId == null) throw new ArgumentNullException(nameof(processId));
            var businessProcess = GetBusinessProcess(processId, BusinessProcessType.MoveIn);

            businessProcess.Effectuate(today);
            var newSupplier = _supplierRegistrations.Find(supplier => supplier.BusinessProcessId.Equals(businessProcess.BusinessProcessId))!;
            newSupplier.StartOfSupply(businessProcess.EffectiveDate);

            var consumer = _consumerRegistrations.Find(consumerRegistration => consumerRegistration.BusinessProcessId.Equals(businessProcess.BusinessProcessId))!;
            consumer.SetMoveInDate(businessProcess.EffectiveDate);

            AddDomainEvent(new ConsumerMovedIn(
                Id.Value,
                GsrnNumber.Value,
                businessProcess.BusinessProcessId.Value,
                businessProcess.EffectiveDate));

            AddDomainEvent(new EnergySupplierChanged(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, newSupplier.EnergySupplierId.Value, businessProcess.EffectiveDate));
        }

        public void CancelChangeOfSupplier(BusinessProcessId processId)
        {
            if (processId is null) throw new ArgumentNullException(nameof(processId));

            var businessProcess = GetBusinessProcess(processId, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Cancel();
            AddDomainEvent(new ChangeOfSupplierCancelled(Id, GsrnNumber, businessProcess.BusinessProcessId));
        }

        public void UpdateConsumerCustomer(BusinessProcessId processId, Customer customer)
        {
            var consumer = GetConsumerRegistration(processId);

            if (consumer is null)
            {
                throw new BusinessProcessException("Can't find consumer registration to update customer on");
            }

            consumer.UpdateCustomer(customer);
        }

        public void UpdateConsumerSecondCustomer(BusinessProcessId processId, Customer customer)
        {
            var consumer = GetConsumerRegistration(processId);

            if (consumer is null)
            {
                throw new BusinessProcessException("Can't find consumer registration to update second customer on");
            }

            consumer.UpdateSecondCustomer(customer);
        }

        private static void StartOfSupplyForFutureSupplier(BusinessProcess businessProcess, SupplierRegistration supplierRegistration)
        {
            supplierRegistration.StartOfSupply(businessProcess.EffectiveDate);
        }

        private ConsumerRegistration? GetConsumerRegistration(BusinessProcessId processId)
        {
            return _consumerRegistrations.Find(consumerRegistration => consumerRegistration.BusinessProcessId.Equals(processId));
        }

        private SupplierRegistration GetFutureSupplierRegistration(BusinessProcess businessProcess)
        {
            var futureSupplier = _supplierRegistrations.Find(s => s.BusinessProcessId.Equals(businessProcess.BusinessProcessId));
            if (futureSupplier == null)
            {
                throw new BusinessProcessException(
                    $"Could find supplier registration of process id {businessProcess.BusinessProcessId.Value}.");
            }

            return futureSupplier;
        }

        private void DiscontinueCurrentSupplier(BusinessProcess businessProcess, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var currentSupplier = GetCurrentSupplier(systemDateTimeProvider);
            if (currentSupplier == null)
            {
                throw new BusinessProcessException($"Could not find current energy supplier.");
            }

            currentSupplier.MarkEndOfSupply(businessProcess.EffectiveDate);
        }

        private SupplierRegistration? GetCurrentSupplier(ISystemDateTimeProvider systemDateTimeProvider)
        {
            return _supplierRegistrations.Find(supplier =>
                supplier.StartOfSupplyDate?.ToDateTimeUtc().Date <= systemDateTimeProvider.Now().ToDateTimeUtc().Date && supplier.EndOfSupplyDate == null);
        }

        private BusinessProcess GetBusinessProcess(BusinessProcessId processId, BusinessProcessType businessProcessType)
        {
            var businessProcess =
                _businessProcesses.Find(p => p.BusinessProcessId.Equals(processId) && p.ProcessType == businessProcessType);
            if (businessProcess == null)
            {
                throw new BusinessProcessException($"Business process ({businessProcessType.Name}) {processId.ToString()} does not exist.");
            }

            return businessProcess;
        }

        private BusinessProcess CreateBusinessProcess(Instant effectiveDate, BusinessProcessType businessProcessType, BusinessProcessId businessProcessId)
        {
            if (_businessProcesses.Any(p => p.BusinessProcessId.Equals(businessProcessId)))
            {
                throw new BusinessProcessException($"Process id {businessProcessId.Value} does already exist.");
            }

            return new BusinessProcess(businessProcessId, effectiveDate, businessProcessType);
        }
    }
}
