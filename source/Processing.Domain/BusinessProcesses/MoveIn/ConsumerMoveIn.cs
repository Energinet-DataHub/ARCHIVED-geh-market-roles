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
using Processing.Domain.Common;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;

namespace Processing.Domain.BusinessProcesses.MoveIn
{
    public class ConsumerMoveIn
    {
        private readonly EffectiveDatePolicy _policy;

        public ConsumerMoveIn(EffectiveDatePolicy policy)
        {
            _policy = policy;
        }

#pragma warning disable CA1822 // Methods should not be static
        public BusinessRulesValidationResult CanStartProcess(AccountingPoint accountingPoint, EffectiveDate consumerMovesInOn, Instant today)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));

            var timePolicyCheckResult = _policy.Check(today, consumerMovesInOn);
            if (timePolicyCheckResult.Success == false)
            {
                return timePolicyCheckResult;
            }

            return accountingPoint.ConsumerMoveInAcceptable(consumerMovesInOn.DateInUtc);
        }

        public void StartProcess(AccountingPoint accountingPoint, Consumer consumer, EnergySupplier energySupplier, EffectiveDate consumerMovesInOn, Transaction transaction, Instant today)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            if (energySupplier == null) throw new ArgumentNullException(nameof(energySupplier));
            if (consumerMovesInOn == null) throw new ArgumentNullException(nameof(consumerMovesInOn));
            accountingPoint.AcceptConsumerMoveIn(consumer.ConsumerId, energySupplier.EnergySupplierId, consumerMovesInOn.DateInUtc, transaction);
        }
    }
}
