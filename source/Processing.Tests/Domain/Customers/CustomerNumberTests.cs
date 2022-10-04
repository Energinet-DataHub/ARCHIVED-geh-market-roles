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

using Processing.Domain.Customers;
using Processing.Domain.Customers.Rules;
using Xunit;

namespace Processing.Tests.Domain.Customers;

public class CustomerNumberTests
{
    [Theory]
    [InlineData("1212567890", true)]
    [InlineData("12345678911", false)]
    [InlineData("aserdfgtyh", false)]
    public void Accept_cpr_number(string cprNumber, bool willAccept)
    {
        Assert.Equal(willAccept, CustomerNumber.Validate(cprNumber).Success);
    }

    [Theory]
    [InlineData("1212567890", CustomerNumber.CustomerNumberType.Cpr)]
    [InlineData("1212120000", CustomerNumber.CustomerNumberType.FictionalCpr)]
    [InlineData("12125678", CustomerNumber.CustomerNumberType.Cvr)]
    [InlineData("11111111", CustomerNumber.CustomerNumberType.FictionalCvr)]
    public void Can_create(string customerNumber, CustomerNumber.CustomerNumberType expectedType)
    {
        var sut = CustomerNumber.Create(customerNumber);

        Assert.Equal(customerNumber, sut.Value);
        Assert.Equal(expectedType, sut.Type);
    }

    [Theory]
    [InlineData("12125678", true)]
    [InlineData("1212567", false)]
    [InlineData("121256789", false)]
    [InlineData("aserdfgtyh", false)]
    public void Accept_cvr_number(string cvrNumber, bool willAccept)
    {
        Assert.Equal(willAccept, CustomerNumber.Validate(cvrNumber).Success);
    }

    [Fact]
    public void Cannot_create()
    {
        Assert.Throws<InvalidCustomerNumberException>(() => CustomerNumber.Create("NOT VALID"));
    }
}
