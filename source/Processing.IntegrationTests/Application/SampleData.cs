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
using NodaTime;
using Processing.IntegrationTests.Factories;

namespace Processing.IntegrationTests.Application
{
    public static class SampleData
    {
        public static string GsrnNumber => "571234567891234568";

        public static string EnergySupplierId => "03b97a60-8145-4599-981f-c4ab5035d978";

        public static string ProcessId => "E8504121-CD44-42A4-9FF5-884809905E06";

        public static string CustomerNumber => "2601211234";

        public static string GlnNumber => "5790000555550";

        public static string ConsumerName => "Test Testesen";

        public static string SecondConsumerName => "Test Testesen 2";

        public static string SecondConsumerNumber => "1005214321";

        public static string MoveInDate => EffectiveDateFactory.AsOfToday().ToString();
    }
}
