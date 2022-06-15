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
using MediatR;
using Processing.Application.Common;
using Processing.Application.Common.Commands;
using Processing.Application.Common.TimeEvents;
using Processing.Application.MoveIn.Processing;
using Processing.Domain.MeteringPoints;

namespace Processing.Application.MoveIn;

public class ProcessPendingProcesses : INotificationHandler<DayHasPassed>
{
    private readonly ICommandScheduler _commandScheduler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbConnectionFactory _connectionFactory;

    public ProcessPendingProcesses(ICommandScheduler commandScheduler, IUnitOfWork unitOfWork, IDbConnectionFactory connectionFactory)
    {
        _commandScheduler = commandScheduler;
        _unitOfWork = unitOfWork;
        _connectionFactory = connectionFactory;
    }

    public async Task Handle(DayHasPassed notification, CancellationToken cancellationToken)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        var sql = $"SELECT Id AS {nameof(PendingProcess.ProcessId)}, AccountingPointId AS {nameof(PendingProcess.AccountingPointId)} FROM [dbo].[BusinessProcesses] " +
                  $"WHERE ProcessType = @ProcessType AND Status = @Status AND EffectiveDate <= @EffectiveDate";
        var pendingBusinessProcesses = await _connectionFactory.GetOpenConnection().QueryAsync<PendingProcess>(
            sql,
            new
            {
                ProcessType = BusinessProcessType.MoveIn.Id,
                Status = BusinessProcessStatus.Pending.Id,
                EffectiveDate = notification.Now.ToDateTimeUtc(),
            }).ConfigureAwait(false);

        foreach (var pendingBusinessProcess in pendingBusinessProcesses)
        {
            var command = new EffectuateConsumerMoveIn(
                pendingBusinessProcess.AccountingPointId,
                pendingBusinessProcess.ProcessId.ToString());
            await _commandScheduler
                .EnqueueAsync(
                    command,
                    BusinessProcessId.Create(pendingBusinessProcess.ProcessId),
                    null).ConfigureAwait(false);
        }

        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }

    private record PendingProcess(Guid ProcessId, Guid AccountingPointId);
}
