﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Processing.Domain.Contracts
{
    public class Contract
    {
        private Contract(
            Customer customer,
            BusinessProcessId businessProcessId,
            EnergySupplierId energySupplierId,
            EffectiveDate effectiveDate,
            AccountingPointId accountingPointId)
        {
            ContractId = Guid.NewGuid();
            BusinessProcessId = businessProcessId;
            EffectiveDate = effectiveDate;
            AccountingPointId = accountingPointId;
            ContractDetails = ContractDetails.Create(customer, energySupplierId);
        }

        public Guid ContractId { get; }

        public BusinessProcessId BusinessProcessId { get; }

        public ContractDetails ContractDetails { get; }

        public EffectiveDate EffectiveDate { get; set; }

        public AccountingPointId AccountingPointId { get; set; }

        internal static Contract Create(Customer customer, BusinessProcessId businessProcessId, EnergySupplierId energySupplierId, EffectiveDate effectiveDate, AccountingPointId accountingPointId)
        {
            return new Contract(customer, businessProcessId, energySupplierId, effectiveDate, accountingPointId);
        }
    }
}
