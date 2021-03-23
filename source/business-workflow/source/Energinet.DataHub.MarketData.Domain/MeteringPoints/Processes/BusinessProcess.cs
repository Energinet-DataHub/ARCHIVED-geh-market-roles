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
using System.Drawing;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes
{
    public abstract class BusinessProcess : Entity
    {
        protected BusinessProcess(MeteringPoint meteringPoint, ProcessId processId, BalanceSupplierId balanceSupplierId, ConsumerId consumerId, Instant effectuationDate)
        {
            MeteringPoint = meteringPoint;
            ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
            BalanceSupplierId = balanceSupplierId;
            ConsumerId = consumerId;
            EffectuationDate = effectuationDate;
            State = BusinessProcessState.Pending;
        }

        public ProcessId ProcessId { get; }

        public BalanceSupplierId? BalanceSupplierId { get; }

        public ConsumerId? ConsumerId { get; }

        public Instant EffectuationDate { get; }

        public BusinessProcessState State { get; protected set; }

        protected MeteringPoint MeteringPoint { get; }

        public void EnsureCompletion(ISystemDateTimeProvider systemDateTimeProvider)
        {
            EnsureEffectuationDate(systemDateTimeProvider);
            EnsureStatusIsPending();
        }

        public abstract void Cancel();

        private void EnsureEffectuationDate(ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (EffectuationDate > systemDateTimeProvider.Now())
            {
                throw new BusinessProcessException($"Cannot complete process ahead of effectuation date.");
            }
        }

        private void EnsureStatusIsPending()
        {
            if (State != BusinessProcessState.Pending)
            {
                throw new BusinessProcessException($"Cannot complete process while status is {State.Name}");
            }
        }
    }
}
