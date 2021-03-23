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
using System.Linq;
using Energinet.DataHub.MarketData.Domain.Customers;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes.ChangeOfSupplier.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public sealed class MeteringPoint : AggregateRootBase
    {
        private readonly MeteringPointType _meteringPointType;
        private readonly bool _isProductionObligated;
        private readonly List<BalanceSupplier> _balanceSuppliers = new List<BalanceSupplier>();
        private readonly List<BusinessProcess> _businessProcesses = new List<BusinessProcess>();
        private PhysicalState _physicalState;
        private List<Consumer> _consumers = new List<Consumer>();

        public MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, BalanceSupplier balanceSupplier)
        {
            GsrnNumber = gsrnNumber;
            _balanceSuppliers.Add(balanceSupplier);
            _meteringPointType = meteringPointType;
            _physicalState = PhysicalState.New;
            AddDomainEvent(new MeteringPointCreated(GsrnNumber, _meteringPointType));
        }

        public MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType)
        {
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = PhysicalState.New;
            AddDomainEvent(new MeteringPointCreated(GsrnNumber, _meteringPointType));
        }

        private MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, bool isProductionObligated)
            : this(gsrnNumber, meteringPointType)
        {
            _isProductionObligated = isProductionObligated;
        }

        private MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, bool isProductionObligated, List<Consumer> consumers, List<BalanceSupplier> balanceSuppliers, int id, int version, PhysicalState physicalState)
        {
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = physicalState;
            _isProductionObligated = isProductionObligated;
            _consumers = consumers;
            Id = id;
            Version = version;
        }

        public GsrnNumber GsrnNumber { get; private set; }

        public static MeteringPoint CreateProduction(GsrnNumber gsrnNumber, bool isObligated)
        {
            return new MeteringPoint(gsrnNumber, MeteringPointType.Production, isObligated);
        }

        public static MeteringPoint CreateFrom(MeteringPointSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new MeteringPoint(
                GsrnNumber.Create(snapshot.GsrnNumber),
                MeteringPointType.FromValue<MeteringPointType>(snapshot.MeteringPointType),
                snapshot.IsProductionObligated,
                snapshot.Consumers.Select(r => Consumer.CreateFrom(r)).ToList(),
                snapshot.BalanceSuppliers.Select(r => BalanceSupplier.CreateFrom(r)).ToList(),
                snapshot.Id,
                snapshot.Version,
                PhysicalState.FromValue<PhysicalState>(snapshot.PhysicalState));
        }

        public BusinessRulesValidationResult CanChangeSupplier(GlnNumber newBalanceSupplierId, Instant effectuationDate, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (newBalanceSupplierId is null)
            {
                throw new ArgumentNullException(nameof(newBalanceSupplierId));
            }

            if (systemDateTimeProvider == null)
            {
                throw new ArgumentNullException(nameof(systemDateTimeProvider));
            }

            var rules = new List<IBusinessRule>()
            {
                new MeteringPointMustBeEnergySuppliableRule(_meteringPointType),
                new ProductionMeteringPointMustBeObligatedRule(_meteringPointType, _isProductionObligated),
                new CannotBeInStateOfClosedDownRule(_physicalState),
                //new MustHaveBalanceSupplierAssociatedRule(BalanceSupplier),
                new ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule(_balanceSuppliers.AsReadOnly(), effectuationDate),
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_consumers.AsReadOnly(), effectuationDate),
                new MoveOutRegisteredOnSameDateIsNotAllowedRule(_consumers.AsReadOnly(), effectuationDate),
                new EffectuationDateCannotBeInThePastRule(effectuationDate, systemDateTimeProvider.Now()),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void InitiateChangeOfSupplier(ProcessId processId, GlnNumber newBalanceSupplierId, Instant effectuationDate, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (CanChangeSupplier(newBalanceSupplierId, effectuationDate, systemDateTimeProvider).AreAnyBroken == true)
            {
                throw new InvalidOperationException();
            }

            var process = new ChangeOfSupplierProcess(
                this,
                processId,
                new BalanceSupplierId(newBalanceSupplierId.Value),
                effectuationDate,
                systemDateTimeProvider);
            _businessProcesses.Add(process);
        }

        public void SetSupplierNotifiedStatus(ProcessId processId)
        {
            if (!(_businessProcesses
                .FirstOrDefault(p => p.ProcessId.Equals(processId)) is ChangeOfSupplierProcess process))
            {
                throw new BusinessProcessNotFoundException(processId);
            }

            process.SetAwaitingEffectuationDateStatus();
        }

        public void CompleteProcess(ProcessId processId, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var process = GetProcessOrThrow<BusinessProcess>(processId);
            process.EnsureCompletion(systemDateTimeProvider);

            if (!(process.BalanceSupplierId is null !))
            {
                SetSupplier(process.BalanceSupplierId!, process.EffectuationDate);
            }
        }

        public void CloseDown()
        {
            if (_physicalState != PhysicalState.ClosedDown)
            {
                _physicalState = PhysicalState.ClosedDown;
                AddDomainEvent(new MeteringPointClosedDown(GsrnNumber));
            }
        }

        public void RegisterMoveIn(ProcessId processId, CustomerId customerId, GlnNumber balanceSupplierId, Instant effectuationDate)
        {
            if (customerId is null)
            {
                throw new ArgumentNullException(nameof(customerId));
            }

            if (balanceSupplierId is null)
            {
                throw new ArgumentNullException(nameof(balanceSupplierId));
            }

            _consumers.Add(new Consumer(customerId, effectuationDate));
            _balanceSuppliers.Add(new BalanceSupplier(new BalanceSupplierId(balanceSupplierId.Value),  effectuationDate));
        }

        public void ActivateMoveIn(ProcessId processId)
        {
            // _consumers.Where(c => c.ProcessId.Equals(processId))
            //     .ToList()
            //     .ForEach(c => c.Activate());

            // _balanceSuppliers
            //     .First(b => b.ProcessId.Equals(processId))
            //     .Activate();
        }

        public void RegisterMoveOut(CustomerId customerId, Instant effectuationDate)
        {
            if (customerId is null)
            {
                throw new ArgumentNullException(nameof(customerId));
            }

            var consumers = _consumers
                .Where(c => c.CustomerId.Equals(customerId))
                .ToList();

            consumers.ForEach(c => c.MoveOut(effectuationDate));
        }

        public MeteringPointSnapshot GetSnapshot()
        {
            var customerRegistrations = _consumers.Select(r => r.GetSnapshot()).ToList();
            var balanceSupplierRegistrations = _balanceSuppliers.Select(r => r.GetSnapshot()).ToList();
            return new MeteringPointSnapshot(
                Id,
                GsrnNumber.Value,
                _meteringPointType.Id,
                customerRegistrations,
                balanceSupplierRegistrations,
                _isProductionObligated,
                _physicalState.Id,
                Version);
        }

        public void CancelProcess(ProcessId processId)
        {
            var process = GetProcessOrThrow<BusinessProcess>(processId);
            process.Cancel();
        }

        public BusinessRulesValidationResult CanCancelSupplierPendingRegistration(string registrationId, GlnNumber energySupplierId)
        {
            throw new NotImplementedException();
        }

        public void CancelSupplierPendingRegistration(string registrationId, GlnNumber energySupplierId)
        {
            throw new NotImplementedException();
        }

        public override IReadOnlyList<IDomainEvent> GetDomainEvents()
        {
            var domainEventsFromProcesses = _businessProcesses.SelectMany(p => p.DomainEvents);
            return DomainEvents.Concat(domainEventsFromProcesses).ToList();
        }

        internal BalanceSupplier GetCurrentSupplier()
        {
            // TODO: How to handle NULL dates
            return _balanceSuppliers
                .FirstOrDefault(s => s.EndOn == NodaConstants.UnixEpoch) !;
        }

        private TProcessType GetProcessOrThrow<TProcessType>(ProcessId processId)
        {
            if (!(_businessProcesses
                .FirstOrDefault(p => p.ProcessId.Equals(processId)) is TProcessType process))
            {
                throw new BusinessProcessNotFoundException(processId);
            }

            return process;
        }

        private void SetSupplier(BalanceSupplierId balanceSupplierId, Instant effectuationDate)
        {
            var currentSupplier = GetCurrentSupplier();
            currentSupplier.End(effectuationDate);

            var newBalanceSupplier = new BalanceSupplier(balanceSupplierId, effectuationDate);
            _balanceSuppliers.Add(newBalanceSupplier);

            AddDomainEvent(new BalanceSupplierChanged(GsrnNumber, currentSupplier.BalanceSupplierId, newBalanceSupplier.BalanceSupplierId, effectuationDate));
        }
    }
}
