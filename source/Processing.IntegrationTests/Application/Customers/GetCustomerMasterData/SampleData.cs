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

using Processing.IntegrationTests.Factories;

namespace Processing.IntegrationTests.Application.Customers.GetCustomerMasterData
{
    public static class SampleData
    {
        public static string GsrnNumber => "571234567891234568";

        public static string MeteringPointId => "0C19F44B-DDDA-4A99-B8E4-6D454D5FD95E";

        public static string EnergySupplierNumber => "5790000555550";

        public static string CustomerName => "Test Testesen";

        public static string SsnTypeName => "CPR";

        public static string VatTypeName => "CVR";

        public static string Ssn => "2601211234";

        public static string Vat => "10000000";

        public static string MoveInDate => EffectiveDateFactory.AsOfToday().ToString();
    }
}
