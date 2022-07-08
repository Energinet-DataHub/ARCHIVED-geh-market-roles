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

namespace Processing.Tests.Domain;

internal static class EffectiveDateFactory
{
    internal static EffectiveDate WithTimeOfDay(DateTime now, int hour, int minute, int second)
    {
        var dateTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);
        return EffectiveDate.Create(dateTime);
    }

    internal static EffectiveDate AsOfToday()
    {
        return EffectiveDate.Create(InstantAsOfToday().ToDateTimeUtc());
    }

    internal static Instant InstantAsOfToday()
    {
        var timeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
        var now = SystemClock.Instance.GetCurrentInstant()
            .InZone(timeZone);
        var localDateTime = new ZonedDateTime(new LocalDateTime(now.Year, now.Month, now.Day, 0, 0, 0), timeZone, now.Offset);
        localDateTime = localDateTime.PlusHours(24);
        return localDateTime.ToInstant();
    }
}
