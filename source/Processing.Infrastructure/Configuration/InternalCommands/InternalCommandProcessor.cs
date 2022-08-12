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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Polly;
using Processing.Application.Common;
using Processing.Application.Common.Commands;
using Processing.Infrastructure.Configuration.Serialization;

namespace Processing.Infrastructure.Configuration.InternalCommands
{
    public class InternalCommandProcessor
    {
        private readonly InternalCommandAccessor _internalCommandAccessor;
        private readonly IJsonSerializer _serializer;
        private readonly CommandExecutor _commandExecutor;
        private readonly ILogger<InternalCommandProcessor> _logger;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly InternalCommandMapper _mapper;

        public InternalCommandProcessor(
            InternalCommandAccessor internalCommandAccessor,
            IJsonSerializer serializer,
            CommandExecutor commandExecutor,
            ILogger<InternalCommandProcessor> logger,
            IDbConnectionFactory connectionFactory,
            InternalCommandMapper mapper)
        {
            _internalCommandAccessor = internalCommandAccessor ?? throw new ArgumentNullException(nameof(internalCommandAccessor));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
            _logger = logger;
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _mapper = mapper;
        }

        public async Task ProcessPendingAsync()
        {
            var pendingCommands = await _internalCommandAccessor.GetPendingAsync().ConfigureAwait(false);

            var executionPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(2),
                });

            foreach (var queuedCommand in pendingCommands)
            {
                var result = await executionPolicy.ExecuteAndCaptureAsync(() =>
                    ExecuteCommandAsync(queuedCommand)).ConfigureAwait(false);

                if (result.Outcome == OutcomeType.Failure)
                {
                    var exception = result.FinalException.ToString();
                    await MarkAsFailedAsync(queuedCommand, exception).ConfigureAwait(false);
                    _logger?.Log(LogLevel.Error, result.FinalException, $"Failed to process internal command {queuedCommand.Id}");
                }
                else
                {
                    await MarkAsProcessedAsync(queuedCommand).ConfigureAwait(false);
                }
            }
        }

        private Task ExecuteCommandAsync(QueuedInternalCommand queuedInternalCommand)
        {
            var commandMetaData = _mapper.GetByName(queuedInternalCommand.Type);
            var command = (InternalCommand)_serializer.Deserialize(queuedInternalCommand.Data, commandMetaData.CommandType);
            return _commandExecutor.ExecuteAsync(command, CancellationToken.None);
        }

        private Task MarkAsFailedAsync(QueuedInternalCommand queuedCommand, string exception)
        {
            var connection = _connectionFactory.GetOpenConnection();
            return connection.ExecuteScalarAsync(
                "UPDATE [dbo].[QueuedInternalCommands] " +
                "SET ProcessedDate = @NowDate, " +
                "ErrorMessage = @Error " +
                "WHERE [Id] = @Id",
                new
                {
                    NowDate = DateTime.UtcNow,
                    Error = exception,
                    queuedCommand.Id,
                });
        }

        private Task MarkAsProcessedAsync(QueuedInternalCommand queuedCommand)
        {
            var connection = _connectionFactory.GetOpenConnection();
            return connection.ExecuteScalarAsync(
                "UPDATE [dbo].[QueuedInternalCommands] " +
                "SET ProcessedDate = @NowDate " +
                "WHERE [Id] = @Id",
                new
                {
                    NowDate = DateTime.UtcNow,
                    queuedCommand.Id,
                });
        }
    }
}
