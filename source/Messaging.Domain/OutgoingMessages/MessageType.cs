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

using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class MessageType : EnumerationType
{
    public static readonly MessageType GenericNotification = new(0, "GenericNotification", MessageCategory.MasterData);
    public static readonly MessageType ConfirmRequestChangeOfSupplier = new(1, nameof(ConfirmRequestChangeOfSupplier), MessageCategory.MasterData);
    public static readonly MessageType RejectRequestChangeOfSupplier = new(2, nameof(RejectRequestChangeOfSupplier), MessageCategory.MasterData);
    public static readonly MessageType AccountingPointCharacteristics = new(3, nameof(AccountingPointCharacteristics), MessageCategory.MasterData);
    public static readonly MessageType CharacteristicsOfACustomerAtAnAP = new(4, nameof(CharacteristicsOfACustomerAtAnAP), MessageCategory.MasterData);
    public static readonly MessageType ConfirmRequestChangeAccountingPointCharacteristics = new(5, nameof(ConfirmRequestChangeAccountingPointCharacteristics), MessageCategory.MasterData);
    public static readonly MessageType RejectRequestChangeAccountingPointCharacteristics = new(6, nameof(RejectRequestChangeAccountingPointCharacteristics), MessageCategory.MasterData);
    public static readonly MessageType NotifyAggregatedMeasureData = new(7, nameof(NotifyAggregatedMeasureData), MessageCategory.Aggregations);

    private MessageType(int id, string name, MessageCategory category)
        : base(id, name)
    {
        Category = category;
    }

    public MessageCategory Category { get; }

    public override string ToString()
    {
        return Name;
    }
}
