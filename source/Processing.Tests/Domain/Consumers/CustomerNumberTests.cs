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

using Processing.Domain.Consumers;
using Xunit;

namespace Processing.Tests.Domain.Consumers;

public class CustomerNumberTests
{
    [Theory]
    [InlineData("1212567890", true)]
    [InlineData("12345678911", false)]
    public void Accept_cpr_number(string cprNumber, bool isValid)
    {
        Assert.Equal(isValid, CustomerNumber.Validate(cprNumber).Success);
    }

    [Theory]
    [InlineData("12125678", true)]
    public void Accept_cvr_number(string cvrNumber, bool isValid)
    {
        Assert.Equal(isValid, CustomerNumber.Validate(cvrNumber).Success);
    }
}
