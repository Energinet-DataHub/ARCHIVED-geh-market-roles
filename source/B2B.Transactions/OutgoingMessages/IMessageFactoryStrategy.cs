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
using System.IO;
using System.Threading.Tasks;

namespace B2B.Transactions.OutgoingMessages
{
    /// <summary>
    /// Creates an outgoing message
    /// </summary>
    public interface IMessageFactoryStrategy
    {
        /// <summary>
        /// Determines whether the factory can create documents of the given type
        /// </summary>
        /// <param name="documentType"></param>
        bool CanHandleDocumentType(string documentType);

        /// <summary>
        /// Creates the message
        /// </summary>
        /// <param name="header"></param>
        /// <param name="marketActivityRecordPayloads"></param>
        Task<Stream> CreateFromAsync(MessageHeader header, IReadOnlyCollection<MarketActivityRecordPayload> marketActivityRecordPayloads);
    }
}
