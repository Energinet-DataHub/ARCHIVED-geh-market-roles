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

using System.Linq;
using FluentValidation;
using Processing.Domain.BusinessProcesses.MoveIn.Errors;
using Processing.Domain.Customers;
using Processing.Domain.MeteringPoints;

namespace Processing.Application.MoveIn.Validation
{
    public class InputValidationSet : AbstractValidator<MoveInRequest>
    {
        public InputValidationSet()
        {
            RuleFor(request => request.Customer.Name)
                .NotEmpty()
                .WithState(_ => new ConsumerNameIsRequired());
            RuleFor(request => request.AccountingPointNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithState(_ => new GsrnNumberIsRequired());
            RuleFor(request => GsrnNumber.CheckRules(request.AccountingPointNumber))
                .Must(x => x.Success)
                .WithState(_ => new InvalidGsrnNumber());
            RuleFor(request => request.Customer.Number)
                .NotEmpty()
                .WithState(_ => new ConsumerIdentifierIsRequired());
            RuleFor(request => CustomerNumber.Validate(request.Customer.Number))
                .Must(result => result.Success)
                .WithState((_, result) => result.Errors.First());
        }
    }
}
