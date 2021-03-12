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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenEnergyHub.Iso8601;
using GreenEnergyHub.Messaging.Dispatching;
using GreenEnergyHub.Queues.ValidationReportDispatcher;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges
{
    /// <summary>
    /// Class which defines how to handle <see cref="ChangeOfChargesMessage"/>.
    /// </summary>
    public class ChangeOfChargesCommandHandler : HubCommandHandler<ChangeOfChargesMessage>
    {
        private readonly ILogger _logger;
        private readonly IValidationReportQueueDispatcher _validationReportQueueDispatcher;
        private readonly IChargeRepository _chargeRepository;
        private readonly IIso8601Durations _iso8601Durations;
        private HubRequestValidationResult? _errorResponse;

        public ChangeOfChargesCommandHandler(
            ILogger<ChangeOfChargesCommandHandler> logger,
            IValidationReportQueueDispatcher validationReportQueueDispatcher,
            IChargeRepository chargeRepository,
            IIso8601Durations iso8601Durations)
        {
            _logger = logger;
            _validationReportQueueDispatcher = validationReportQueueDispatcher;
            _chargeRepository = chargeRepository;
            _iso8601Durations = iso8601Durations;
        }

        protected override async Task AcceptAsync(ChangeOfChargesMessage actionData, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(ChangeOfChargesMessage)} have parsed validation");

            if (actionData.MktActivityRecord == null) { throw new ArgumentException($"{nameof(actionData.MktActivityRecord)} fails as an argument is null"); }
            if (actionData.Period?.Resolution == null) { throw new ArgumentException($"{nameof(actionData.Period.Resolution)} fails as an argument is null"); }
            if (actionData.Period.Points == null) { throw new ArgumentException($"{nameof(actionData.Period.Points)} fails as an argument is null"); }

            foreach (var point in actionData.Period.Points)
            {
                var time = _iso8601Durations.AddDuration(actionData.MktActivityRecord.ValidityStartDate, actionData.Period.Resolution, point.Position);
                point.Time = time;
            }

            actionData.CorrelationId = Guid.NewGuid().ToString();
            actionData.LastUpdatedBy = "someone";

            await _chargeRepository.StoreChargeAsync(actionData).ConfigureAwait(false);
        }

        protected override Task OnErrorAsync(Exception innerException)
        {
            // TODO: On error, send message to some dead-letter queue
            throw innerException;
        }

        protected override async Task RejectAsync(ChangeOfChargesMessage actionData, CancellationToken cancellationToken)
        {
            await _validationReportQueueDispatcher.DispatchAsync(_errorResponse).ConfigureAwait(false);
        }

        protected override Task<bool> ValidateAsync(ChangeOfChargesMessage changeOfChargesMessage, CancellationToken cancellationToken)
        {
            _errorResponse = new HubRequestValidationResult(changeOfChargesMessage.Transaction.MRID);
            ChangeOfChargesValidationRules.Validate(changeOfChargesMessage, _errorResponse);

            return Task.FromResult(!_errorResponse.Errors.Any());
        }
    }
}
