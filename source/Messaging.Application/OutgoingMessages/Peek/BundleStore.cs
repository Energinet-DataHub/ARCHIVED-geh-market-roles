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
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

public class BundleStore : IBundleStore
{
    private readonly IDbConnectionFactory? _connectionFactory;

    public BundleStore()
    {
    }

    public BundleStore(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<Stream?> GetBundleOfAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);

        return _connectionFactory!
            .GetOpenConnection()
            .QuerySingleOrDefaultAsync<Stream?>($"SELECT Bundle FROM b2b.BundleStore WHERE Id = @Id", new
            {
                Id = GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver),
            });
    }

    public async Task SetBundleForAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver,
        Stream document)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);

        var sql = @$"UPDATE [B2B].[BundleStore]
                     SET Bundle = @Bundle
                     WHERE Id = @Id
                     AND Bundle IS NULL";
        var result = await _connectionFactory!
            .GetOpenConnection()
            .ExecuteAsync(
                sql,
                new
                {
                    Bundle = document,
                    Id = GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver),
                }).ConfigureAwait(false);

        if (result == 0) throw new BundleException("Fail to store bundle on registration: " + GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver));
    }

    public async Task<bool> TryRegisterBundleAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);

        var bundleRegistrationStatement = $"IF NOT EXISTS (SELECT * FROM b2b.BundleStore WHERE Id = @Id)" +
                                          $"INSERT INTO b2b.BundleStore(Id) VALUES(@Id)";
        var result = await _connectionFactory!
            .GetOpenConnection().ExecuteAsync(
                bundleRegistrationStatement,
                new
                {
                    @Id = GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver),
                })
            .ConfigureAwait(false);

        return result == 1;
    }

    private static string GenerateKey(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        return messageCategory.Name + messageReceiverNumber.Value + roleOfReceiver.Name;
    }
}
