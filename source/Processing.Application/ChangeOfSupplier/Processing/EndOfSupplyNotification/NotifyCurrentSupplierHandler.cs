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
using Processing.Application.Common.DomainEvents;
using Processing.Domain.MeteringPoints;

namespace Processing.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification
{
    public class NotifyCurrentSupplierHandler : ICommandHandler<NotifyCurrentSupplier>
    {
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly IEndOfSupplyNotifier _endOfSupplyNotifier;

        public NotifyCurrentSupplierHandler(IDomainEventPublisher domainEventPublisher, IEndOfSupplyNotifier endOfSupplyNotifier)
        {
            _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
            _endOfSupplyNotifier = endOfSupplyNotifier ?? throw new ArgumentNullException(nameof(endOfSupplyNotifier));
        }

        public async Task<Unit> Handle(NotifyCurrentSupplier request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            await _endOfSupplyNotifier.NotifyAsync(AccountingPointId.Create(request.AccountingPointId)).ConfigureAwait(false);

            await _domainEventPublisher.PublishAsync(new CurrentSupplierNotified(
                AccountingPointId.Create(request.AccountingPointId),
                BusinessProcessId.Create(request.BusinessProcessId))).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
