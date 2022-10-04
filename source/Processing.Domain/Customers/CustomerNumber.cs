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
using Processing.Domain.Customers.Rules;
using Processing.Domain.SeedWork;

namespace Processing.Domain.Customers
{
    public class CustomerNumber : ValueObject
    {
        private CustomerNumber(string customerNumber)
        {
            Value = customerNumber;
            Type = DetermineType();
        }

        public enum CustomerNumberType
        {
            Cpr,
            FictionalCpr,
            Cvr,
            FictionalCvr,
        }

        public string Value { get; }

        public CustomerNumberType Type { get; }

        public static CustomerNumber Create(string customerNumber)
        {
            if (Validate(customerNumber).Success == false)
            {
                throw new InvalidCustomerNumberException($"{customerNumber} is not a valid customer number");
            }

            return new CustomerNumber(customerNumber);
        }

        public static BusinessRulesValidationResult Validate(string customerNumber)
        {
            ArgumentNullException.ThrowIfNull(customerNumber);
            if (IsCprNumber(customerNumber))
            {
                return ValidateCprNumber(customerNumber);
            }

            if (IsCvrNumber(customerNumber))
            {
                return ValidateCvrNumber(customerNumber);
            }

            return BusinessRulesValidationResult.Failed(new InvalidCustomerNumber());
        }

        private static BusinessRulesValidationResult ValidateCvrNumber(string customerNumber)
        {
            return new BusinessRulesValidationResult(new List<IBusinessRule>() { new CvrNumberFormatRule(customerNumber), });
        }

        private static BusinessRulesValidationResult ValidateCprNumber(string customerNumber)
        {
            return new BusinessRulesValidationResult(new List<IBusinessRule>() { new CprNumberFormatRule(customerNumber), });
        }

        private static bool IsCvrNumber(string customerNumber)
        {
            return customerNumber.Length == 8;
        }

        private static bool IsCprNumber(string customerNumber)
        {
            return customerNumber.Length == 10;
        }

        private CustomerNumberType DetermineType()
        {
            if (IsCprNumber(Value))
            {
                return IsFictionalCpr() ? CustomerNumberType.FictionalCpr : CustomerNumberType.Cpr;
            }

            return IsFictionalCvr() ? CustomerNumberType.FictionalCvr : CustomerNumberType.Cvr;
        }

        private bool IsFictionalCvr()
        {
            return Value == "11111111";
        }

        private bool IsFictionalCpr()
        {
            return Value.Substring(Value.Length - 4) == "0000";
        }
    }
}
