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
        public BusinessProcessResult(string transactionId, IEnumerable<IBusinessRule> businessRules)
        {
            TransactionId = transactionId;
            SetValidationErrors(businessRules);
            Success = ValidationErrors.Count == 0;
        }

        public BusinessProcessResult(string transactionId, IBusinessRule businessRule)
        {
            if (businessRule == null) throw new ArgumentNullException(nameof(businessRule));

            TransactionId = transactionId;
            SetValidationErrors(new List<IBusinessRule>() { businessRule });
            Success = ValidationErrors.Count == 0;
        }

        public BusinessProcessResult(string transactionId, IReadOnlyCollection<ValidationError> validationErrors)
        {
            TransactionId = transactionId;
            ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
            Success = ValidationErrors.Count == 0;
        }

        private BusinessProcessResult(string transactionId, bool success, string processId)
        {
            TransactionId = transactionId;
            Success = success;
            ProcessId = processId;
        }

        public bool Success { get; }

        public string TransactionId { get; }

        public IReadOnlyCollection<ValidationError> ValidationErrors { get; private set; } = new List<ValidationError>();

        public string? ProcessId { get; set; }

        public static BusinessProcessResult Ok(string transactionId, string processId)
        {
            return new BusinessProcessResult(transactionId, true, processId);
        }

        public static BusinessProcessResult Fail(string transactionId, params ValidationError[] validationErrors)
        {
            return new BusinessProcessResult(transactionId, validationErrors);
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
