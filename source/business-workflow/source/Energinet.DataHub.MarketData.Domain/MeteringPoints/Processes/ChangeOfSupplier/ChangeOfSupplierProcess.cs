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
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes.ChangeOfSupplier.Events;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes.ChangeOfSupplier
{
    public class ChangeOfSupplierProcess : BusinessProcess
    {
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public ChangeOfSupplierProcess(MeteringPoint meteringPoint, ProcessId processId, BalanceSupplierId balanceSupplierId, Instant effectuationDate, ISystemDateTimeProvider systemDateTimeProvider)
            : base(meteringPoint, processId, balanceSupplierId, null!, effectuationDate)
        {
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            AddDomainEvent(new ChangeOfSupplierInitiated(processId, balanceSupplierId, effectuationDate));
            SetAwaitingSupplierNotificationStatus();
        }

        public void SetAwaitingEffectuationDateStatus()
        {
            if (State != ChangeOfSupplierProcessState.AwaitingSupplierNotification)
            {
                throw new BusinessProcessException(
                    $"Cannot change status to {ChangeOfSupplierProcessState.Pending} while current status is {State.Name}.");
            }

            State = ChangeOfSupplierProcessState.Pending;
            AddDomainEvent(new StateChangedToAwaitingEffectuationDate(ProcessId, BalanceSupplierId!, EffectuationDate));
        }

        public override void Cancel()
        {
            if (State == BusinessProcessState.Completed)
            {
                throw new BusinessProcessException($"Cannot cancel process while status is {State.Name}.");
            }

            State = BusinessProcessState.Cancelled;
            AddDomainEvent(new ChangeOfSupplierProcessCancelled(ProcessId));
        }

        private void SetAwaitingSupplierNotificationStatus()
        {
            Instant notificationDate;
            if (EffectuationDate.Equals(_systemDateTimeProvider.Now()))
            {
                notificationDate = _systemDateTimeProvider.Now();
            }
            else
            {
                // TODO: Inject threshold from a policy
                notificationDate = EffectuationDate.Minus(Duration.FromHours(72));
            }

            State = ChangeOfSupplierProcessState.AwaitingSupplierNotification;
            AddDomainEvent(new StateChangedToAwaitingNotifySupplier(ProcessId, new BalanceSupplierId(MeteringPoint.GetCurrentSupplier().BalanceSupplierId!.Value), EffectuationDate, notificationDate));
        }
    }
}
