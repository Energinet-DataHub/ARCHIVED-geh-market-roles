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
using System.Collections.ObjectModel;
using NodaTime;
using NodaTime.Text;
using Processing.Domain.SeedWork;

namespace Processing.Domain.Common
{
    public class EffectiveDate : ValueObject
    {
        private EffectiveDate(Instant dateInUtc)
        {
            DateInUtc = dateInUtc;
        }

        public Instant DateInUtc { get; }

        public static BusinessRulesValidationResult CheckRules(string dateInUtc)
        {
            var rules = new Collection<IBusinessRule>()
            {
                new DateFormatMustBeUTCRule(dateInUtc),
            };
            return new BusinessRulesValidationResult(rules);
        }

        public static EffectiveDate Create(DateTime date)
        {
            var utcDate = Instant.FromDateTimeUtc(DateTime.SpecifyKind(date, DateTimeKind.Utc));
            return Create(utcDate.ToString());
        }

        public static EffectiveDate Create(string dateInUtc)
        {
            if (dateInUtc == null) throw new ArgumentNullException(nameof(dateInUtc));

            dateInUtc = dateInUtc.Trim();

            if (CheckRules(dateInUtc).Success == false)
            {
                throw new InvalidEffectiveDateFormat(dateInUtc);
            }

            var parseResult = InstantPattern.General.Parse(dateInUtc);
            if (parseResult.Success == false)
            {
                throw new InvalidEffectiveDateFormat(dateInUtc);
            }

            return new EffectiveDate(parseResult.Value);
        }

        public override string ToString()
        {
            return DateInUtc.ToString();
        }
    }
}
