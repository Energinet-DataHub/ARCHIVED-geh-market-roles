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
using System.Globalization;
using Processing.Domain.Common;
using Xunit;
using Xunit.Categories;

namespace Processing.Tests.Domain.Common
{
    [UnitTest]
    public class EffectiveDateTests
    {
        [Theory]
        [InlineData("2021-06-01T22:00:00Z", true)]
        [InlineData("2021-12-30T23:00:00Z", true)]
        [InlineData("2021-06-01T22:00:00.000Z", true)]
        [InlineData("2021-06-01T22:00:00.100Z", false)]
        [InlineData("2021-06-01T23:01:00Z", false)]
        [InlineData("2021-06-01T00:00:00Z", false)]
        [InlineData("2021-06-01T00:00:00.000Z", false)]
        [InlineData("2021-09-25T22:00:00Z", true)]
        [InlineData("2021-06-01", false)]
        [InlineData("Not a date", false)]
        public void Date_format_must_be_able_to_handle_daylight_savings(string dateString, bool isValid)
        {
            var result = EffectiveDate.CheckRules(dateString);

            Assert.Equal(isValid, result.Success);
        }

        [Fact]
        public void Create_should_succeed_when_date_format_is_valid()
        {
            var dateString = DaylightSavingsString(new DateTime(2021, 6, 1));
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
            var date = DaylightSavingsAdjusted(DateTime.Now);
            var effectiveDate = EffectiveDate.Create(date);

            Assert.NotNull(effectiveDate);
            Assert.Equal(date, effectiveDate.DateInUtc.ToDateTimeUtc());
        }

        private static DateTime DaylightSavingsAdjusted(DateTime date)
        {
            var info = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var isDaylightSavingTime = info.IsDaylightSavingTime(date);

            return new DateTime(
                date.Year,
                date.Month,
                date.Day,
                isDaylightSavingTime ? 22 : 23,
                0,
                0);
        }

        private static string DaylightSavingsString(DateTime date)
        {
            // setting the hour to 20 so that dates will never change
            var dateForString = new DateTime(
                date.Year,
                date.Month,
                date.Day,
                20,
                date.Minute,
                date.Second,
                date.Millisecond);

            var info = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var isDaylightSavingTime = info.IsDaylightSavingTime(dateForString);

            var retVal = dateForString.ToString(
                isDaylightSavingTime
                    ? $"yyyy'-'MM'-'dd'T'22':'mm':'ss'Z'"
                    : "yyyy'-'MM'-'dd'T'23':'mm':'ss'Z'",
                CultureInfo.InvariantCulture);

            return retVal;
        }
    }
}
