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
using Energinet.DataHub.MeteringPoints.IntegrationEvents.CreateMeteringPoint;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Processing.Application.AccountingPoints;
using Processing.Domain.MeteringPoints;
using Processing.Infrastructure.Configuration.InternalCommands;

namespace Processing.Api.EventListeners;

public class MeteringPointCreatedListener
{
    private readonly CommandSchedulerFacade _commandScheduler;
    private readonly ILogger<MeteringPointCreatedListener> _logger;

    public MeteringPointCreatedListener(
        CommandSchedulerFacade commandScheduler,
        ILogger<MeteringPointCreatedListener> logger)
    {
        _commandScheduler = commandScheduler;
        _logger = logger;
    }

    [Function("MeteringPointCreatedListener")]
    public Task RunAsync(
        [ServiceBusTrigger("%INTEGRATION_EVENT_TOPIC_NAME%", "%METERING_POINT_CREATED_EVENT_ENERGY_SUPPLYING_SUBSCRIPTION_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data,
        FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogInformation($"Received metering point created integration event");

        var meteringPointCreatedEvent = MeteringPointCreated.Parser.ParseFrom(data);
        if (IsAccountingPoint(meteringPointCreatedEvent))
        {
            var command = new CreateAccountingPoint(
                meteringPointCreatedEvent.MeteringPointId,
                meteringPointCreatedEvent.GsrnNumber,
                ParseAccountPointTypeFrom(meteringPointCreatedEvent));
            return _commandScheduler.EnqueueAsync(command);
        }

        _logger.LogInformation($"Metering point type {meteringPointCreatedEvent.MeteringPointType.ToString()} is not an accounting point type. Accounting point creation is skipped.");
        return Task.CompletedTask;
    }

    private static string ParseAccountPointTypeFrom(MeteringPointCreated meteringPointCreatedEvent)
    {
        return meteringPointCreatedEvent.MeteringPointType == MeteringPointCreated.Types.MeteringPointType.MptConsumption ? MeteringPointType.Consumption.Name : MeteringPointType.Production.Name;
    }

    private static bool IsAccountingPoint(MeteringPointCreated meteringPointCreatedEvent)
    {
        return meteringPointCreatedEvent.MeteringPointType ==
               MeteringPointCreated.Types.MeteringPointType.MptConsumption ||
               meteringPointCreatedEvent.MeteringPointType == MeteringPointCreated.Types.MeteringPointType.MptProduction;
    }
}
