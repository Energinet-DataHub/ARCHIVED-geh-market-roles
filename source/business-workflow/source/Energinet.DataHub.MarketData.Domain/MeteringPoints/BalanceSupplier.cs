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
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Processes;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public class BalanceSupplier : Entity
    {
        public BalanceSupplier(BalanceSupplierId balanceSupplierId, Instant startOn)
        {
            BalanceSupplierId = balanceSupplierId ?? throw new ArgumentNullException(nameof(balanceSupplierId));
            StartOn = startOn;
        }

        private BalanceSupplier(int id, BalanceSupplierId balanceSupplierId, Instant startOn, Instant endOn)
        {
            Id = id;
            BalanceSupplierId = balanceSupplierId ?? throw new ArgumentNullException(nameof(balanceSupplierId));
            StartOn = startOn;
            EndOn = endOn;
        }

        public BalanceSupplierId BalanceSupplierId { get; }

        public Instant StartOn { get; }

        public Instant EndOn { get; private set; }

        public static BalanceSupplier CreateFrom(BalanceSupplierSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new BalanceSupplier(
                snapshot.Id,
                new BalanceSupplierId(snapshot.BalanceSupplierId),
                snapshot.StartOn,
                snapshot.EndOn);
        }

        public BalanceSupplierSnapshot GetSnapshot()
        {
            return new BalanceSupplierSnapshot(
                Id,
                BalanceSupplierId.Value,
                StartOn,
                EndOn);
        }

        public void End(Instant effectuationDate)
        {
            EndOn = effectuationDate;
        }
    }
}
