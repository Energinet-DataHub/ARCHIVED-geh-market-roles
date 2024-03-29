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
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.CimMessageAdapter.Messages;

namespace Messaging.Infrastructure.IncomingMessages
{
    public class TransactionIdRegistry : ITransactionIds
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public TransactionIdRegistry(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<bool> TryStoreAsync(string transactionId)
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);

            var result = await connection.ExecuteAsync(
                    $"IF NOT EXISTS (SELECT * FROM b2b.TransactionIds WHERE TransactionId = @TransactionId)" +
                    $"INSERT INTO b2b.TransactionIds(TransactionId) VALUES(@TransactionId)",
                    new { TransactionId = transactionId })
                .ConfigureAwait(false);

            return result == 1;
        }
    }
}
