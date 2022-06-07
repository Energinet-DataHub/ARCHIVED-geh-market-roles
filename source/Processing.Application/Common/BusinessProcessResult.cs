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
using System.Collections.Generic;
using System.Linq;
using Processing.Domain.SeedWork;

namespace Processing.Application.Common
{
    public class BusinessProcessResult
    {
        public BusinessProcessResult(IEnumerable<IBusinessRule> businessRules)
        {
            SetValidationErrors(businessRules);
            Success = ValidationErrors.Count == 0;
        }

        public BusinessProcessResult(IBusinessRule businessRule)
        {
            if (businessRule == null) throw new ArgumentNullException(nameof(businessRule));

            SetValidationErrors(new List<IBusinessRule>() { businessRule });
            Success = ValidationErrors.Count == 0;
        }

        public BusinessProcessResult(IReadOnlyCollection<ValidationError> validationErrors)
        {
            ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
            Success = ValidationErrors.Count == 0;
        }

        private BusinessProcessResult(bool success, string processId)
        {
            Success = success;
            ProcessId = processId;
        }

        public bool Success { get; }

        public IReadOnlyCollection<ValidationError> ValidationErrors { get; private set; } = new List<ValidationError>();

        public string ProcessId { get; } = string.Empty;

        public static BusinessProcessResult Ok(string processId)
        {
            return new BusinessProcessResult(true, processId);
        }

        public static BusinessProcessResult Fail(params ValidationError[] validationErrors)
        {
            return new BusinessProcessResult(validationErrors);
        }

        private void SetValidationErrors(IEnumerable<IBusinessRule> rules)
        {
            ValidationErrors = rules
                .Where(r => r.IsBroken)
                .Select(r => r.ValidationError)
                .ToList();
        }
    }
}
