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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Processing.Application.EnergySuppliers;
using Processing.Infrastructure.Configuration.InternalCommands;

namespace Processing.Api.EventListeners
{
    public class ActorCreatedListener
    {
        private readonly CommandSchedulerFacade _commandSchedulerFacade;
        private readonly ILogger<ActorCreatedListener> _logger;

        public ActorCreatedListener(CommandSchedulerFacade commandSchedulerFacade, ILogger<ActorCreatedListener> logger)
        {
            _commandSchedulerFacade = commandSchedulerFacade;
            _logger = logger;
        }

        [Function("ActorCreatedListener")]
        public async Task RunAsync([ServiceBusTrigger("%INTEGRATION_EVENT_TOPIC_NAME%", "%MARKET_PARTICIPANT_CHANGED_ACTOR_CREATED_SUBSCRIPTION_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data, FunctionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var integrationEvent = SharedIntegrationEventContract.Parser.ParseFrom(data);

            _logger.LogInformation($"actor created event received for actor: {integrationEvent.ActorCreatedIntegrationEvent.Name}");

            var isEnergySupplier = integrationEvent.ActorCreatedIntegrationEvent.BusinessRoles.Contains(12);

            if (isEnergySupplier)
            {
                _logger.LogInformation($" {integrationEvent.ActorCreatedIntegrationEvent.Name} businessRoles contained role 12 (Energy Supplier)");
                var command = new CreateEnergySupplier(
                    integrationEvent.ActorCreatedIntegrationEvent.ActorId,
                    integrationEvent.ActorCreatedIntegrationEvent.ActorNumber);

                await _commandSchedulerFacade.EnqueueAsync(command).ConfigureAwait(false);
            }
        }
    }
}
