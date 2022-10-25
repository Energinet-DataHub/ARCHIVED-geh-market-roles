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

namespace Processing.IntegrationTests.Application.ChangeCustomerCharacteristics
{
    public static class SampleData
    {
        public static string TransactionId => "1665742827671";

        public static string ProcessId => "561F819D-A858-4346-87C4-3268310BAD48";

        public static string MarketEvaluationPointId => Processing.IntegrationTests.Application.SampleData.GsrnNumber;

        public static string EffectiveDate => "2022-09-30 22:00:00.0000000";

        public static string CurrentEnergySupplierId => "5178861303303";

        public static string State => "Started";

        public static string StartedByMessageId => "1665742823675";

        public static string NewEnergySupplierId => "5178861303303";

        public static string ConsumerId => "0902742529";

        public static string ConsumerName => Processing.IntegrationTests.Application.SampleData.ConsumerName;

        public static string ConsumerIdType => "ARR";

        public static string CurrentEnergySupplierNotificationState => "WasNotified";

        public static string MeteringPointMasterDataState => "Sent";

        public static string CustomerMasterDataState => "Sent";

        public static string BusinessProcessState => "Completed";

        public static string GridOperatorNotificationState => "WasNotified";

        public static string GridOperatorMessageDeliveryStateCustomerMasterData => "Sent";

        public static string CustomerMasterData => "{\"MarketEvaluationPoint\":\"578142272442666086\",\"ElectricalHeating\":false,\"ElectricalHeatingStart\":null,\"FirstCustomerId\":\"\",\"FirstCustomerName\":\"HansHansen\",\"SecondCustomerId\":\"\",\"SecondCustomerName\":\"\",\"ProtectedName\":false,\"HasEnergySupplier\":false,\"SupplyStart\":\"2022-09-30T22:00:00Z\"}";
    }
}
