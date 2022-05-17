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

using NodaTime;
using Processing.Domain.BusinessProcesses.MoveIn;

namespace Processing.Tests.Domain;

internal static class EffectiveDatePolicyFactory
{
    private static readonly DateTimeZone _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    internal static TimeOfDay CreateDefaultTimeOfDay()
    {
        return TimeOfDay.Create(0, 0, 0);
    }

    internal static EffectiveDatePolicy CreateEffectiveDatePolicy()
    {
        return new EffectiveDatePolicy(0, 0, CreateDefaultTimeOfDay(), _dateTimeZone);
    }

    internal static EffectiveDatePolicy CreateEffectiveDatePolicy(TimeOfDay timeOfDay)
    {
        return new EffectiveDatePolicy(0, 0, timeOfDay, _dateTimeZone);
    }

    internal static EffectiveDatePolicy CreateEffectiveDatePolicy(int allowedNumberOfDaysBeforeToday, int allowedNumberOfDaysAfterToday = 0)
    {
        return new EffectiveDatePolicy(allowedNumberOfDaysBeforeToday, allowedNumberOfDaysAfterToday, CreateDefaultTimeOfDay(), _dateTimeZone);
    }
}
