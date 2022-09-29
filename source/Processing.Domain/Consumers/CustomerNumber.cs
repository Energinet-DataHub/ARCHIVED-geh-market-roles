using System;
using System.Collections.Generic;
using Processing.Domain.Consumers.Rules;
using Processing.Domain.SeedWork;

namespace Processing.Domain.Consumers
{
    public class CustomerNumber : ValueObject
    {
        private CustomerNumber(string cprNumber)
        {
            Value = cprNumber;
        }

        public string Value { get; }

        public static CustomerNumber Create(string cprNumber)
        {
            return new CustomerNumber(cprNumber);
        }

        public static BusinessRulesValidationResult Validate(string customerNumber)
        {
            ArgumentNullException.ThrowIfNull(customerNumber);
            if (IsCprNumber(customerNumber))
            {
                var rules = new List<IBusinessRule>() { new CprNumberFormatRule(customerNumber), };
                return new BusinessRulesValidationResult(rules);
            }

            if (customerNumber.Length == 8)
            {
                return BusinessRulesValidationResult.Succeeded();
            }

            return BusinessRulesValidationResult.Failed(new CprNumberFormatRuleError(customerNumber));
        }

        private static bool IsCprNumber(string customerNumber)
        {
            return customerNumber.Length == 10;
        }
    }
}
