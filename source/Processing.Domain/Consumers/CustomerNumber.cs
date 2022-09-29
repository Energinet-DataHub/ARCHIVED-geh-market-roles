using System;
using System.Collections.Generic;
using Processing.Domain.Consumers.Rules;
using Processing.Domain.SeedWork;

namespace Processing.Domain.Consumers
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
            FictionalCvr,
        }

        public string Value { get; }

        public CustomerNumberType Type { get; }

        public static CustomerNumber Create(string cprNumber)
        {
            return new CustomerNumber(cprNumber);
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

            return BusinessRulesValidationResult.Failed(new CprNumberFormatRuleError(customerNumber));
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

            return CustomerNumberType.FictionalCvr;
        }

        private bool IsFictionalCpr()
        {
            return Value.Substring(Value.Length - 4) == "0000";
        }
    }
}
