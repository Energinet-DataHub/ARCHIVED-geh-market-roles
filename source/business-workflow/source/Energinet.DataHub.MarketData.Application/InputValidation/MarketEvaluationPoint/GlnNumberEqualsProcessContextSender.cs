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

using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using FluentValidation.Validators;
using GreenEnergyHub.Messaging.Validation;

namespace Energinet.DataHub.MarketData.Application.InputValidation.MarketEvaluationPoint
{
    public class GlnNumberEqualsProcessContextSender : PropertyRule<string>
    {
        private readonly ProcessContext _context;

        public GlnNumberEqualsProcessContextSender(ProcessContext context)
        {
            _context = context;
        }

        protected override string Code => "E16";

        protected override bool IsValid(string propertyValue, PropertyValidatorContext context)
        {
            var glnNumber = new GlnNumber(propertyValue);
            return glnNumber.Equals(_context.SenderGlnNumber);
        }
    }
}
