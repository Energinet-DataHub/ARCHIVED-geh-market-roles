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
using System.Threading.Tasks;
using Processing.Application.Common.Commands;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.Configuration.Correlation;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.Serialization;

namespace Processing.Infrastructure.Configuration.InternalCommands
{
    public class CommandScheduler : ICommandScheduler
    {
        private readonly MarketRolesContext _context;
        private readonly IJsonSerializer _serializer;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly InternalCommandMapper _internalCommandMapper;

        public CommandScheduler(MarketRolesContext context, IJsonSerializer serializer, ISystemDateTimeProvider systemDateTimeProvider, ICorrelationContext correlationContext, InternalCommandMapper internalCommandMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _internalCommandMapper = internalCommandMapper;
        }

        public async Task EnqueueAsync<TCommand>(TCommand command)
            where TCommand : InternalCommand
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var data = _serializer.Serialize(command);
            var commandMetadata = _internalCommandMapper.GetByType(command.GetType());
            var queuedCommand = new QueuedInternalCommand(command.Id, commandMetadata.CommandName, data, _systemDateTimeProvider.Now());
            await _context.QueuedInternalCommands.AddAsync(queuedCommand).ConfigureAwait(false);
        }
    }
}
