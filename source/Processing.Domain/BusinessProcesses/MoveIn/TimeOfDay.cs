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
using NodaTime;
using Processing.Domain.Common;
using Processing.Domain.SeedWork;

namespace Processing.Domain.BusinessProcesses.MoveIn
{
    public class TimeOfDay : ValueObject
    {
        private readonly int _hour;
        private readonly int _minute;
        private readonly int _second;

        private TimeOfDay(int hour, int minute, int second)
        {
            _hour = hour;
            _minute = minute;
            _second = second;
        }

        public static TimeOfDay Create(int hour, int minute, int second)
        {
            return new TimeOfDay(hour, minute, second);
        }

        public static TimeOfDay Create(EffectiveDate effectiveDate, DateTimeZone dateTimeZone)
        {
            if (effectiveDate == null) throw new ArgumentNullException(nameof(effectiveDate));
            var zonedDatetime = effectiveDate.DateInUtc.InZone(dateTimeZone);
            return new TimeOfDay(zonedDatetime.Hour, zonedDatetime.Minute, zonedDatetime.Minute);
        }
    }
}
