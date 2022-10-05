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
using Processing.Domain.Customers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;

namespace Processing.Domain.BusinessProcesses.MoveIn
{
    public class CustomerMoveIn
    {
        private readonly EffectiveDatePolicy _policy;

        public CustomerMoveIn(EffectiveDatePolicy policy)
        {
            _policy = policy;
        }

#pragma warning disable CA1822 // Methods should not be static
        public BusinessRulesValidationResult CanStartProcess(
            AccountingPoint accountingPoint,
            EffectiveDate consumerMovesInOn,
            Instant today,
            Customer customer)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));
            if (customer == null) throw new ArgumentNullException(nameof(customer));

            var timePolicyCheckResult = _policy.Check(today, consumerMovesInOn);
            if (timePolicyCheckResult.Success == false)
            {
                return timePolicyCheckResult;
            }

            return accountingPoint.ConsumerMoveInAcceptable(consumerMovesInOn.DateInUtc, customer, today);
        }

        public void StartProcess(AccountingPoint accountingPoint, EnergySupplier energySupplier, EffectiveDate consumerMovesInOn, Instant today, BusinessProcessId businessProcessId, Customer customer)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));
            if (energySupplier == null) throw new ArgumentNullException(nameof(energySupplier));
            if (consumerMovesInOn == null) throw new ArgumentNullException(nameof(consumerMovesInOn));
            accountingPoint.RegisterMoveIn(customer, energySupplier.EnergySupplierId, consumerMovesInOn.DateInUtc, businessProcessId, today);
            if (EffectiveDateIsInThePast(consumerMovesInOn, today))
            {
                accountingPoint.EffectuateConsumerMoveIn(businessProcessId, today);
            }
        }

        private static bool EffectiveDateIsInThePast(EffectiveDate consumerMovesInOn, Instant today)
        {
            return consumerMovesInOn.DateInUtc < today;
        }
    }
}
