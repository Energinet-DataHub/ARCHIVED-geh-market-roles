﻿// Copyright 2020 Energinet DataHub A/S
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
using NodaTime;
using Processing.Domain.BusinessProcesses.MoveIn.Errors;
using Processing.Domain.Common;
using Processing.Domain.SeedWork;

namespace Processing.Domain.BusinessProcesses.MoveIn
{
    public class EffectiveDatePolicy
    {
        private readonly TimeOfDay _requiredTimeOfDayInLocalTime;
        private readonly DateTimeZone _timeZone;
        private readonly int _allowedNumberOfDaysBeforeToday;
        private readonly int _allowedNumberOfDaysAfterToday;

        public EffectiveDatePolicy(int allowedNumberOfDaysBeforeToday, int allowedNumberOfDaysAfterToday, TimeOfDay requiredTimeOfDayInLocalTime, DateTimeZone timeZone)
        {
            _allowedNumberOfDaysBeforeToday = allowedNumberOfDaysBeforeToday;
            _allowedNumberOfDaysAfterToday = allowedNumberOfDaysAfterToday;
            _requiredTimeOfDayInLocalTime = requiredTimeOfDayInLocalTime;
            _timeZone = timeZone;
        }

        public BusinessRulesValidationResult Check(Instant today, EffectiveDate effectiveDate)
        {
            if (effectiveDate == null) throw new ArgumentNullException(nameof(effectiveDate));
            var maxDifferenceInDays = EffectiveDateIsBeforeToday(today, effectiveDate)
                ? _allowedNumberOfDaysBeforeToday
                : _allowedNumberOfDaysAfterToday;

            if (EffectiveDateIsWithinAllowedTimePeriod(today, effectiveDate, maxDifferenceInDays) == false)
            {
                return BusinessRulesValidationResult.Failed(new EffectiveDateIsNotWithinAllowedTimePeriod());
            }

            if (TimeOfDayIsValid(effectiveDate) == false)
            {
              return BusinessRulesValidationResult.Failed(new InvalidEffectiveDateTimeOfDay());
            }

            return BusinessRulesValidationResult.Succeeded();
        }

        private static bool EffectiveDateIsBeforeToday(Instant today, EffectiveDate effectiveDate)
        {
            return ToDate(effectiveDate.DateInUtc) < ToDate(today);
        }

        private static bool EffectiveDateIsWithinAllowedTimePeriod(Instant today, EffectiveDate effectiveDate, int maxDifferenceInDays)
        {
            return !(DifferenceInDays(today, effectiveDate) > maxDifferenceInDays);
        }

        private static int DifferenceInDays(Instant today, EffectiveDate effectiveDate)
        {
            var todayDatetime = ToDate(today);
            var effectiveDateTime = ToDate(effectiveDate.DateInUtc);

            if (todayDatetime > effectiveDateTime)
            {
                return (todayDatetime - effectiveDateTime).Days;
            }

            return (effectiveDateTime - todayDatetime).Days;
        }

        private static DateTime ToDate(Instant instant)
        {
            return instant.ToDateTimeUtc().Date;
        }

        private bool TimeOfDayIsValid(EffectiveDate effectiveDate)
        {
            var timeOfDay = TimeOfDay.Create(effectiveDate, _timeZone);
            return timeOfDay.Equals(_requiredTimeOfDayInLocalTime);
        }
    }
}
