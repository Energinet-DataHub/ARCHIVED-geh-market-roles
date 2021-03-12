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

using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using NodaTime;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges
{
    public static class ChangeOfChargesValidationRules
    {
        private static readonly int StartOfValidIntervalFromNowInDays = 31;
        private static readonly int EndOfValidIntervalFromNowInDays = 1095;

        public static void Validate(ChangeOfChargesMessage changeOfChargesMessage, HubRequestValidationResult errorResponse)
        {
            var validateStartDateVr209Result = ValidateStartDateVr209(changeOfChargesMessage);
            if (validateStartDateVr209Result != null)
            {
                errorResponse.Add(validateStartDateVr209Result);
            }
        }

        private static ValidationError? ValidateStartDateVr209(ChangeOfChargesMessage changeOfChargesMessage)
        {
            var startOfValidInterval = SystemClock.Instance.GetCurrentInstant()
                .Plus(Duration.FromDays(StartOfValidIntervalFromNowInDays));
            var endOfValidInterval = SystemClock.Instance.GetCurrentInstant()
                .Plus(Duration.FromDays(EndOfValidIntervalFromNowInDays));
            var startDate = changeOfChargesMessage.MktActivityRecord?.ValidityStartDate;
            var success = startDate >= startOfValidInterval && startDate <= endOfValidInterval;

            return success ? null : new ValidationError("VR209", "Time limits not followed");
        }
    }
}
