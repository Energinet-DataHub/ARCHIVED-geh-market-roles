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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.OutgoingMessages.Peek;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.OutgoingMessages;

public class OutgoingMessageEnqueuer
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IUnitOfWork _unitOfWork;

    public OutgoingMessageEnqueuer(IDatabaseConnectionFactory databaseConnectionFactory, IUnitOfWork unitOfWork)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task EnqueueMessagesAsync(ReadOnlyCollection<EnqueuedMessage> messages)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        const string sql = @"INSERT INTO [B2B].[EnqueuedMessages] (Id, MessageType, MessageCategory,ReceiverId, ReceiverRole, SenderId, SenderRole, ProcessType,MessageRecord)
        SELECT (Id, MessageType, MessageCategory,ReceiverId, ReceiverRole, SenderId, SenderRole, ProcessType,MessageRecord)
        FROM @TVP;";

        using var connection = (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        using var command = connection.CreateCommand();
        using var dt = new DataTable("EnqueuedMessageType", "dbo");

        dt.Columns.AddRange(new[]
        {
            new DataColumn("Id", typeof(Guid)),
            new DataColumn("MessageType", typeof(string)),
            new DataColumn("MessageCategory", typeof(string)),
            new DataColumn("ReceiverId", typeof(string)),
            new DataColumn("ReceiverRole", typeof(string)),
            new DataColumn("SenderId", typeof(string)),
            new DataColumn("SenderRole", typeof(string)),
            new DataColumn("ProcessType", typeof(string)),
            new DataColumn("MessageRecord", typeof(string)),
        });

        foreach (var message in messages)
        {
            dt.Rows.Add(message.Id, message.MessageType, message.Category, message.ReceiverId, message.ReceiverRole, message.SenderId, message.SenderRole, message.ProcessType, message.MessageRecord);
        }

        command.CommandText = sql;
        var parameter = command.Parameters.AddWithValue("TVP", dt);
        parameter.SqlDbType = SqlDbType.Structured;
        parameter.TypeName = "dbo.EnqueuedMessageType";

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task EnqueueAsync(EnqueuedMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var sql = @$"INSERT INTO [B2B].[EnqueuedMessages] VALUES (@Id, @MessageType, @MessageCategory, @ReceiverId, @ReceiverRole, @SenderId, @SenderRole, @ProcessType, @MessageRecord)";

        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        await connection
            .ExecuteAsync(
                sql,
                new
                {
                    Id = message.Id,
                    MessageType = message.MessageType,
                    MessageCategory = message.Category,
                    ReceiverId = message.ReceiverId,
                    ReceiverRole = message.ReceiverRole,
                    SenderId = message.SenderId,
                    SenderRole = message.SenderRole,
                    ProcessType = message.ProcessType,
                    MessageRecord = message.MessageRecord,
                },
                _unitOfWork.CurrentTransaction).ConfigureAwait(false);
    }
}
