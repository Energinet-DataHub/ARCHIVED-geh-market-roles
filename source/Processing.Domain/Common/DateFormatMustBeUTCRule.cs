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

using System.Text.RegularExpressions;
using Processing.Domain.SeedWork;

namespace Processing.Domain.Common
{
    public class DateFormatMustBeUTCRule : IBusinessRule
    {
        private const string UtcFormat = @"\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[1-2]\d|3[0-1])T(?:[0-1]\d|2[0-3]):[0-5]\d:[0-5]\dZ$";
        private readonly string _date;

        public DateFormatMustBeUTCRule(string date)
        {
            IsBroken = !Regex.IsMatch(date, UtcFormat);
            _date = date;
        }

        public bool IsBroken { get; }

        public ValidationError ValidationError => new DateFormatMustBeUTCRuleError(_date);
    }
}
