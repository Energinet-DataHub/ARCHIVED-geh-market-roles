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

using System.IO;
using System.Threading.Tasks;
using Messaging.Application.IncomingMessages;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.CimMessageAdapter.Messages;

/// <summary>
/// Parses CIM messages from a stream
/// </summary>
public interface IMessageParser<TMarketActivityRecordType, TMarketTransactionType>
    where TMarketActivityRecordType : IMarketActivityRecord
    where TMarketTransactionType : IMarketTransaction<TMarketActivityRecordType>
{
    /// <summary>
    /// The CIM format handled
    /// </summary>
    MessageFormat HandledFormat { get; }

    /// <summary>
    /// Parse from stream
    /// </summary>
    /// <param name="message"></param>
    Task<MessageParserResult<TMarketActivityRecordType, TMarketTransactionType>> ParseAsync(Stream message);
}
