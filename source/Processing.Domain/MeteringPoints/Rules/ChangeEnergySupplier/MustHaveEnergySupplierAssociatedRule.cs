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

using Processing.Domain.EnergySuppliers;
using Processing.Domain.SeedWork;

namespace Processing.Domain.MeteringPoints.Rules.ChangeEnergySupplier
{
    public class MustHaveEnergySupplierAssociatedRule : IBusinessRule
    {
        private readonly EnergySupplierId? _energySupplierId;

        internal MustHaveEnergySupplierAssociatedRule(EnergySupplierId? energySupplierId)
        {
            _energySupplierId = energySupplierId;
        }

        public bool IsBroken => _energySupplierId is null;

        public ValidationError ValidationError => new MustHaveEnergySupplierAssociatedRuleError();
    }
}
