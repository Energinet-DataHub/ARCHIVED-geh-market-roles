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
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Xunit;

namespace Messaging.IntegrationTests.Assertions;

public class AssertQueuedCommand
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly string _commandPayload;
    private readonly CommandMetadata _commandMetadata;

    private AssertQueuedCommand(IDatabaseConnectionFactory connectionFactory, string commandPayload, CommandMetadata commandMetadata)
    {
        _connectionFactory = connectionFactory;
        _commandPayload = commandPayload;
        _commandMetadata = commandMetadata;
    }

    public static AssertQueuedCommand QueuedCommand<TCommandType>(IDatabaseConnectionFactory connectionFactory, InternalCommandMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        var commandMetadata = mapper.GetByType(typeof(TCommandType));
        var sql =
            $"SELECT Data FROM [b2b].[QueuedInternalCommands] WHERE Type = @CommandType";
        var commandPayload = connectionFactory.GetConnectionAndOpen().QuerySingleOrDefault<string>(
            sql,
            new { CommandType = commandMetadata.CommandName, });

        Assert.NotNull(commandPayload);
        return new AssertQueuedCommand(connectionFactory, commandPayload, commandMetadata);
    }

    public object Command()
    {
        return JsonSerializer.Deserialize(_commandPayload, _commandMetadata.CommandType)!;
    }
}
