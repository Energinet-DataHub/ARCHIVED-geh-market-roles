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
using System.Text.Json.Serialization;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;

namespace Messaging.Application.IncomingMessages.RequestChangeOfSupplier
{
    public class RequestChangeOfSupplierTransaction : ICommand<Unit>, IMarketTransaction<MarketActivityRecord>
    {
        [JsonConstructor]
        public RequestChangeOfSupplierTransaction(MessageHeader message, MarketActivityRecord marketActivityRecord)
        {
            Message = message;
            MarketActivityRecord = marketActivityRecord;
        }

        public MessageHeader Message { get; }

        public MarketActivityRecord MarketActivityRecord { get; }

        public static RequestChangeOfSupplierTransaction Create(MessageHeader messageHeader, MarketActivityRecord marketActivityRecord)
        {
            if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
            return new RequestChangeOfSupplierTransaction(messageHeader, marketActivityRecord);
        }
    }
}
