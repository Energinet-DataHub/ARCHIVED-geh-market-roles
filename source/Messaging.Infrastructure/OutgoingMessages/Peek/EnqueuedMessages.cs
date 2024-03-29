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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Infrastructure.OutgoingMessages.Peek;

public class EnqueuedMessages : IEnqueuedMessages
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IBundleConfiguration _bundleConfiguration;

    public EnqueuedMessages(IDatabaseConnectionFactory connectionFactory, IBundleConfiguration bundleConfiguration)
    {
        _connectionFactory = connectionFactory;
        _bundleConfiguration = bundleConfiguration;
    }

    public async Task<MessageRecords?> GetByAsync(ActorNumber actorNumber, MessageCategory messageCategory)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(actorNumber);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var oldestMessage = await FindOldestMessageAsync(actorNumber, messageCategory, connection).ConfigureAwait(false);

        if (oldestMessage is null)
        {
            return null;
        }

        var sqlStatement =
            @$"SELECT TOP({_bundleConfiguration.MaxNumberOfPayloadsInBundle})
            Id AS {nameof(EnqueuedMessage.Id)},
            ReceiverId AS {nameof(EnqueuedMessage.ReceiverId)},
            ReceiverRole AS {nameof(EnqueuedMessage.ReceiverRole)},
            SenderId AS {nameof(EnqueuedMessage.SenderId)},
            SenderRole AS {nameof(EnqueuedMessage.SenderRole)},
            MessageType AS {nameof(EnqueuedMessage.MessageType)},
            MessageCategory AS {nameof(EnqueuedMessage.Category)},
            ProcessType AS {nameof(EnqueuedMessage.ProcessType)},
            MessageRecord FROM [b2b].[EnqueuedMessages]
            WHERE ProcessType = @ProcessType AND ReceiverId = @ReceiverId AND ReceiverRole = @ReceiverRole AND MessageType = @MessageType AND MessageCategory = @MessageCategory";

        var messages = await connection.QueryAsync<EnqueuedMessage>(
            sqlStatement,
            new
            {
                ReceiverRole = oldestMessage.ReceiverRole,
                ProcessType = oldestMessage.ProcessType,
                MessageType = oldestMessage.MessageType,
                MessageCategory = messageCategory.Name,
                ReceiverId = actorNumber.Value,
            }).ConfigureAwait(false);

        return MessageRecords.Create(
            messages.ToList());
    }

    public async Task<int> GetAvailableMessageCountAsync(ActorNumber actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        return await connection.QuerySingleAsync<int>(
            @"SELECT count(*) from [b2b].[EnqueuedMessages] WHERE ReceiverId = @ActorNumber",
            new
            {
                ActorNumber = actorNumber.Value,
            }).ConfigureAwait(false);
    }

    private static async Task<OldestMessage?> FindOldestMessageAsync(ActorNumber actorNumber, MessageCategory messageCategory, IDbConnection connection)
    {
        return await connection
            .QuerySingleOrDefaultAsync<OldestMessage>(
                @$"SELECT TOP(1) {nameof(OldestMessage.ProcessType)}, {nameof(OldestMessage.MessageType)}, {nameof(OldestMessage.ReceiverRole)} FROM [b2b].[EnqueuedMessages]
                WHERE ReceiverId = @ReceiverId AND MessageCategory = @MessageCategory",
                new
            {
                MessageCategory = messageCategory.Name,
                ReceiverId = actorNumber.Value,
            })
            .ConfigureAwait(false);
    }

    private record OldestMessage(string ProcessType, string MessageType, string ReceiverRole);
}
