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
using FluentValidation;
using Processing.Application.Common.Validation;
using Processing.Application.Common.Validation.Consumers;
using Processing.Domain.BusinessProcesses.MoveIn.Errors;

namespace Processing.Application.MoveIn.Validation
{
    public class InputValidationSet : AbstractValidator<MoveInRequest>
    {
        public InputValidationSet()
        {
            RuleFor(request => request.Consumer.Name)
                .NotEmpty()
                .WithState(_ => new ConsumerNameIsRequired());
            RuleFor(request => request.AccountingPointGsrnNumber)
                .NotEmpty()
                .WithState(_ => new GsrnNumberIsRequired());
            RuleFor(request => request.Consumer.Identifier)
                .NotEmpty()
                .WithState(_ => new ConsumerIdentifierIsRequired());
            When(request => request.Consumer.Type.Equals(ConsumerIdentifierType.CPR, StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(request => request.Consumer.Identifier)
                    .SetValidator(new SocialSecurityNumberMustBeValid());
            });
            When(request => request.Consumer.Type.Equals(ConsumerIdentifierType.CVR, StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(request => request.Consumer.Identifier)
                    .SetValidator(new VATNumberMustBeValidRule());
            });
        }
    }
}
