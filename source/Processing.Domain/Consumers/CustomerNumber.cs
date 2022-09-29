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

        public static BusinessRulesValidationResult Validate(string cprNumber)
        {
            var rules = new List<IBusinessRule>() { new CprNumberFormatRule(cprNumber), };
            return new BusinessRulesValidationResult(rules);
        }
    }
}
