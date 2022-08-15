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
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.GenericNotification;
using Messaging.Domain.OutgoingMessages;
using NodaTime;

namespace Messaging.Application.Transactions.MoveIn;

public class MoveInNotifications
{
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;

    public MoveInNotifications(IOutgoingMessageStore outgoingMessageStore, IMarketActivityRecordParser marketActivityRecordParser)
    {
        _outgoingMessageStore = outgoingMessageStore;
        _marketActivityRecordParser = marketActivityRecordParser;
    }

    public void InformCurrentEnergySupplierAboutEndOfSupply(string transactionId, Instant effectiveDate, string marketEvaluationPointId, string energySupplierId)
    {
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transactionId,
            marketEvaluationPointId,
            effectiveDate);

        var message = new OutgoingMessage(
            DocumentType.GenericNotification.ToString(),
            energySupplierId,
            Guid.NewGuid().ToString(),
            transactionId,
            BusinessReasonCode.CustomerMoveInOrMoveOut.Code,
            MarketRoles.EnergySupplier,
            DataHubDetails.IdentificationNumber,
            MarketRoles.MeteringPointAdministrator,
            _marketActivityRecordParser.From(marketActivityRecord),
            null);

        _outgoingMessageStore.Add(message);
    }
}
