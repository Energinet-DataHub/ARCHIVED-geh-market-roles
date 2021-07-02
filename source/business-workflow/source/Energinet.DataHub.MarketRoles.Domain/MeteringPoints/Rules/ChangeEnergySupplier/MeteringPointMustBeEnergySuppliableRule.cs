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

using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier
{
    public class MeteringPointMustBeEnergySuppliableRule : IBusinessRule
    {
        private readonly MeteringPointType _meteringPointType;

        internal MeteringPointMustBeEnergySuppliableRule(MeteringPointType meteringPointType)
        {
            _meteringPointType = meteringPointType;
        }

        public bool IsBroken => !(_meteringPointType == MeteringPointType.Production ||
                                  _meteringPointType == MeteringPointType.Consumption);

        public ValidationError ValidationError => new MeteringPointMustBeEnergySuppliableRuleError();
    }
}
