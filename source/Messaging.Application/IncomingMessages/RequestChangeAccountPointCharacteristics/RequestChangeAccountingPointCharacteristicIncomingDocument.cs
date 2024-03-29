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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics;

public class RequestChangeAccountingPointCharacteristicIncomingDocument : IIncomingMarketDocument<MarketActivityRecord, RequestChangeAccountingPointCharacteristicsTransaction>
{
    public RequestChangeAccountingPointCharacteristicIncomingDocument(MessageHeader header, IReadOnlyCollection<MarketActivityRecord> marketActivityRecords)
    {
        Header = header;
        MarketActivityRecords = marketActivityRecords;
    }

    public MessageHeader Header { get; }

    public IReadOnlyCollection<MarketActivityRecord> MarketActivityRecords { get; }

    public ReadOnlyCollection<RequestChangeAccountingPointCharacteristicsTransaction> ToTransactions()
    {
        var transactions = MarketActivityRecords
            .Select(marketActivityRecord => new RequestChangeAccountingPointCharacteristicsTransaction(Header, marketActivityRecord))
            .ToList();

        return transactions.AsReadOnly();
    }
}
