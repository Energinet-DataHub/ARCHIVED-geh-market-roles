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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages;
using Energinet.DataHub.MarketData.Application.Common;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Cancel
{
    public class PublishCancelMessageHandlerBehavior : IPipelineBehavior<CancelPendingRegistration, BusinessProcessResult>
    {
        private readonly IActorMessagePublisher _publisher;

        public PublishCancelMessageHandlerBehavior(IActorMessagePublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public async Task<BusinessProcessResult> Handle(CancelPendingRegistration request, CancellationToken cancellationToken, RequestHandlerDelegate<BusinessProcessResult> next)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (next == null) throw new ArgumentNullException(nameof(next));

            var result = await next().ConfigureAwait(false);

            if (result.Succeeded)
            {
                await _publisher.PublishAsync(new CancelPendingRegistrationApproved(
                    messageId: "INSERT MESSAGE ID HERE",
                    transactionId: request.TransactionId,
                    meteringPointId: request.MeteringPointId,
                    requestingEnergySupplierId: request.BalanceSupplierId))
                    .ConfigureAwait(false);
            }
            else
            {
                await _publisher.PublishAsync(new CancelPendingRegistrationRejected(
                        messageId: "INSERT MESSAGE ID HERE",
                        transactionId: request.TransactionId,
                        meteringPointId: request.MeteringPointId,
                        requestingEnergySupplierId: request.BalanceSupplierId,
                        reasonCodes: result.Errors ?? new List<string>()))
                    .ConfigureAwait(false);
            }

            return result;
        }
    }
}
