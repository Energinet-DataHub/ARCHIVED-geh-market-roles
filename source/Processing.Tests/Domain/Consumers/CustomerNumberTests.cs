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

using System.Collections.Generic;
using Processing.Domain.SeedWork;
using Xunit;

namespace Processing.Tests.Domain.Consumers;

public class CustomerNumberTests
{
    [Theory]
    [InlineData("1234567890", true)]
    public void Accept_cpr_number(string cprNumber, bool isValid)
    {
        Assert.Equal(isValid, CustomerNumber.Validate(cprNumber).Success);
    }
}

#pragma warning disable
public class CustomerNumber : ValueObject
{
    private CustomerNumber(string cprNumber)
    {
        Value = cprNumber;
    }

    public static CustomerNumber Create(string cprNumber)
    {
        return new CustomerNumber(cprNumber);
    }

    public string Value { get; }

    public static BusinessRulesValidationResult Validate(string cprNumber)
    {
        return BusinessRulesValidationResult.Succeeded();
    }
}
