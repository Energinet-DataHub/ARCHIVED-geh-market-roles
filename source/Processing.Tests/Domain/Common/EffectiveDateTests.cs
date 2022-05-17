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
using Processing.Domain.Common;
using Xunit;
using Xunit.Categories;

namespace Processing.Tests.Domain.Common
{
    [UnitTest]
    public class EffectiveDateTests
    {
        [Theory]
        [InlineData("2021-12-30T23:00:00Z", true)]
        [InlineData("2021-06-01T22:00:00.000Z", false)]
        [InlineData("2021-06-01", false)]
        [InlineData("Not a date", false)]
        public void Must_be_utc_format(string dateString, bool isValid)
        {
            var result = EffectiveDate.CheckRules(dateString);

            Assert.Equal(isValid, result.Success);
        }

        [Fact]
        public void Create_should_succeed_when_date_format_is_valid()
        {
            var dateString = "2021-12-30T23:00:00Z";
            var effectiveDate = EffectiveDate.Create(dateString);

            Assert.NotNull(effectiveDate);
            Assert.Equal(dateString, effectiveDate.ToString());
        }

        [Fact]
        public void Create_should_throw_exception_when_format_is_invalid()
        {
            var invalidDate = "2021-06-01";
            Assert.Throws<InvalidEffectiveDateFormat>(() => EffectiveDate.Create(invalidDate));
        }

        [Fact]
        public void Create_should_succeed_when_date_is_passed()
        {
            var date = DateTime.Now;
            var effectiveDate = EffectiveDate.Create(date);

            Assert.NotNull(effectiveDate);
        }
    }
}
