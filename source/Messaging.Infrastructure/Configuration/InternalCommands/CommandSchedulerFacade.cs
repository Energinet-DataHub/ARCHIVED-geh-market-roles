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

using System.Threading.Tasks;
using Messaging.Application.Common.Commands;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;

namespace Messaging.Infrastructure.Configuration.InternalCommands;

public class CommandSchedulerFacade
{
    private readonly ICommandScheduler _commandScheduler;
    private readonly IUnitOfWork _unitOfWork;

    public CommandSchedulerFacade(ICommandScheduler commandScheduler, IUnitOfWork unitOfWork)
    {
        _commandScheduler = commandScheduler;
        _unitOfWork = unitOfWork;
    }

    public async Task EnqueueAsync(InternalCommand command)
    {
        await _commandScheduler.EnqueueAsync(command).ConfigureAwait(false);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
