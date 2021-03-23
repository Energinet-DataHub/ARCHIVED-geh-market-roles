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

using System.Collections.Generic;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages
{
    public class CancelPendingRegistrationRejected
    {
        public CancelPendingRegistrationRejected(string messageId, string transactionId, string meteringPointId, string requestingEnergySupplierId, IReadOnlyList<string> reasonCodes)
        {
            MessageId = messageId;
            TransactionId = transactionId;
            MeteringPointId = meteringPointId;
            RequestingEnergySupplierId = requestingEnergySupplierId;
            ReasonCodes = reasonCodes;
        }

        public CancelPendingRegistrationRejected()
            : this(string.Empty, string.Empty, string.Empty, string.Empty, new List<string>())
        {
        }

        public string MessageId { get; set; }

        public string TransactionId { get; set; }

        public string MeteringPointId { get; set; }

        public string RequestingEnergySupplierId { get; set; }

        public IReadOnlyList<string> ReasonCodes { get; set; }
    }
}
