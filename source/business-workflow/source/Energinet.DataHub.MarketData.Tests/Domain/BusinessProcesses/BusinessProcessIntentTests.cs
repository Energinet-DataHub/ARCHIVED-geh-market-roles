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
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.BusinessProcesses
{
    [Trait("Category", "Unit")]
    public class BusinessProcessIntentTests
    {
        public static IEnumerable<object[]> INPUT_DATA_FOR_BusinessProcessIntent_HasFlag_Should_Return_Expected()
        {
            yield return new object[] { BusinessProcessIntent.Customer, BusinessProcessIntent.AllIntents, false };
            yield return new object[] { BusinessProcessIntent.Supplier, BusinessProcessIntent.AllIntents, false };
            yield return new object[] { BusinessProcessIntent.AllIntents, BusinessProcessIntent.Customer, true };
            yield return new object[] { BusinessProcessIntent.AllIntents, BusinessProcessIntent.Supplier, true };
            yield return new object[] { BusinessProcessIntent.Customer, BusinessProcessIntent.Customer, true };
            yield return new object[] { BusinessProcessIntent.Customer, BusinessProcessIntent.Supplier, false };
        }

        [Theory]
        [MemberData(nameof(INPUT_DATA_FOR_BusinessProcessIntent_HasFlag_Should_Return_Expected))]
        public void BusinessProcessIntent_HasFlag_Should_Return_Expected(
            BusinessProcessIntent intent,
            BusinessProcessIntent flag,
            bool expected)
        {
            intent.HasFlag(flag).Should().Be(expected);
        }
    }
}
