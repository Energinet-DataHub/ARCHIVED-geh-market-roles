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
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.UpdateCustomer;

public class AssertTransaction
{
    private readonly dynamic _transaction;

    private AssertTransaction(dynamic transaction)
    {
        Assert.NotNull(transaction);
        _transaction = transaction;
    }

    public static async Task<AssertTransaction> TransactionAsync(string transactionId, IEdiDatabaseConnection ediConnection)
    {
        if (ediConnection == null) throw new ArgumentNullException(nameof(ediConnection));
        using var connection = await ediConnection.GetConnectionAndOpenAsync().ConfigureAwait(false);
        return new AssertTransaction(GetTransaction(transactionId, connection));
    }

    private static dynamic? GetTransaction(string transactionId, IDbConnection connection)
    {
        return connection.QuerySingle(
            $"SELECT * FROM b2b.UpdateCustomerMasterDataTransactions WHERE TransactionId = @TransactionId",
            new
            {
                TransactionId = transactionId,
            });
    }
}
