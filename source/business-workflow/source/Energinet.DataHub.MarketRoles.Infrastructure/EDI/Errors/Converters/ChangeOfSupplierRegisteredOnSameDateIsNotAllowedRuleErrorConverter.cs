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

using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleErrorConverter : ErrorConverter<ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleError>
    {
        protected override ErrorMessage Convert(ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleError validationError)
        {
            return new("D07", $"Effectuation date <1>(TODO: Insert effectuation date) incorrect: There is already another market transaction that takes precedence on date <2> (TODO: Insert effectuation date");
        }
    }
}
