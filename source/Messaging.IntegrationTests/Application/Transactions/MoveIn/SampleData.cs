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
using System.Collections.Generic;
using Messaging.Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Domain.Transactions;
using Messaging.IntegrationTests.Factories;
using NodaTime;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

internal static class SampleData
{
    internal static string MeteringPointNumber => "571234567891234568";

    internal static string CurrentEnergySupplierNumber => "5790000555550";

    internal static string NewEnergySupplierNumber => "5790000555551";

    internal static Guid TransactionId => Guid.Parse("8BA514FA-2E4D-4CB7-8B4A-1B1137185BD7");

    internal static ActorProvidedId ActorProvidedId => ActorProvidedId.Create("123456987");

    internal static string MarketEvaluationPointId => "e17fe44f-ea4e-47e0-bbb0-64bfb382922a";

    internal static string OriginalMessageId => "EDE97146-C592-489A-B23A-3C73C096E368";

    internal static string ConsumerId => "12341234";

    internal static string ConsumerName => "John Doe";

    internal static string ConsumerIdType => "ARR";

    internal static string ReceiverId => "5790001330552";

    internal static string SenderId => "5790000555551";

    internal static bool ElectricalHeating => false;

    internal static Instant ElectricalHeatingStart => EffectiveDateFactory.InstantAsOfToday();

    internal static bool ProtectedName => false;

    internal static bool HasEnergySupplier => true;

    internal static Instant SupplyStart => EffectiveDateFactory.InstantAsOfToday();

    internal static IEnumerable<UsagePointLocation> UsagePointLocations => new List<UsagePointLocation>();

    internal static Guid IdOfGridOperatorForMeteringPoint => Guid.Parse("E754226C-3A5C-4E04-A1D4-6FE58782FDC2");

    internal static string NumberOfGridOperatorForMeteringPoint => "1234567890123";
}
