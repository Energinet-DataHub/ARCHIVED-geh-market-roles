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
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages;
using Energinet.DataHub.MarketData.Application.Common;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier
{
    public class PublishActorMessageHandlerBehavior : IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>
    {
        private readonly IActorMessagePublisher _messagePublisher;

        public PublishActorMessageHandlerBehavior(IActorMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        }

        public async Task<RequestChangeOfSupplierResult> Handle(RequestChangeOfSupplier command, CancellationToken cancellationToken, RequestHandlerDelegate<RequestChangeOfSupplierResult> next)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var result = await next().ConfigureAwait(false);
            if (result.Succeeded)
            {
                await PublishAcceptedMessageAsync(command).ConfigureAwait(false);
            }
            else
            {
                await PublishRejectionMessageAsync(command, result).ConfigureAwait(false);
            }

            return result;
        }

        private Task PublishAcceptedMessageAsync(RequestChangeOfSupplier command)
        {
            // TODO: <INSERT MESSAGE ID> will be replaced in another PR
            var message = new RequestChangeOfSupplierApproved("<INSERT MESSAGE ID>", command.Transaction.MRID, command.MarketEvaluationPoint.MRid, command.EnergySupplier.MRID!);
            return SendMessageAsync(message);
        }

        private Task PublishRejectionMessageAsync(RequestChangeOfSupplier command, RequestChangeOfSupplierResult result)
        {
            // TODO: <INSERT MESSAGE ID> will be replaced in another PR
            var message = new RequestChangeOfSupplierRejected("<INSERT MESSAGE ID>", command.Transaction.MRID, command.MarketEvaluationPoint.MRid, command.EnergySupplier.MRID!, result.Errors!);
            return SendMessageAsync(message);
        }

        private Task SendMessageAsync<TMessage>(TMessage message)
        {
            return _messagePublisher.PublishAsync(message);
        }
    }
}
