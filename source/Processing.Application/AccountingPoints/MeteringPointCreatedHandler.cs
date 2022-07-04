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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Processing.Application.Common.Commands;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;

namespace Processing.Application.AccountingPoint
{
    public class MeteringPointCreatedHandler : INotificationHandler<MeteringPointCreated>
    {
        private readonly ICommandScheduler _commandScheduler;

        public MeteringPointCreatedHandler(ICommandScheduler commandScheduler)
        {
            _commandScheduler = commandScheduler;
        }

        public async Task Handle(MeteringPointCreated notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            if (notification.MeteringPointType == MeteringPointType.Consumption ||
                notification.MeteringPointType == MeteringPointType.Production)
            {
                var command = new CreateAccountingPoint(
                    notification.MeteringPointId,
                    notification.GsrnNumber,
                    notification.MeteringPointType.ToString());

                await _commandScheduler.EnqueueAsync(command).ConfigureAwait(false);
            }
        }
    }
}