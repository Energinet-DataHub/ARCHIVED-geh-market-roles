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
using FluentValidation;
using NodaTime;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Validation;
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Infrastructure.RequestAdapters;
using SimpleInjector;

namespace Processing.Infrastructure.Configuration
{
    public static class MoveInProcessRegistrations
    {
        public static void ConfigureMoveIn(
            this Container container,
            int allowedNumberOfDaysBeforeToday,
            int allowedNumberOfDaysAfterToday,
            TimeOfDay requiredTimeOfDayInLocalTime)
        {
            ArgumentNullException.ThrowIfNull(container);

            container.Register<JsonMoveInAdapter>(Lifestyle.Scoped);
            container.Register<IValidator<MoveInRequest>, InputValidationSet>(Lifestyle.Scoped);
            ConfigureMoveInProcessTimePolicy(container, allowedNumberOfDaysBeforeToday, allowedNumberOfDaysAfterToday, requiredTimeOfDayInLocalTime);
        }

        private static void ConfigureMoveInProcessTimePolicy(Container container, int allowedNumberOfDaysBeforeToday, int allowedNumberOfDaysAfterToday, TimeOfDay requiredTimeOfDayInLocalTime)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            container.RegisterInstance<EffectiveDatePolicy>(
                new EffectiveDatePolicy(allowedNumberOfDaysBeforeToday, allowedNumberOfDaysAfterToday, requiredTimeOfDayInLocalTime, DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]));
        }
    }
}
