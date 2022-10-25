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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EnergySupplying.RequestResponse.Requests;
using Google.Protobuf;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NodaTime.Serialization.Protobuf;
using Processing.Application.Customers.GetCustomerMasterData;

namespace Processing.Api.CustomerMasterData
{
    public class CustomerMasterDataRequestListener
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly ServiceBusSender _serviceBusSender;

        public CustomerMasterDataRequestListener(
            ILogger logger,
            IMediator mediator,
            ServiceBusSender serviceBusSender)
        {
            _logger = logger;
            _mediator = mediator;
            _serviceBusSender = serviceBusSender;
        }

        [Function("CustomerMasterDataRequestListener")]
        public async Task RunAsync(
            [ServiceBusTrigger("%CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data,
            FunctionContext context)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var correlationId = ParseCorrelationIdFromMessage(context);
            var request = CustomerMasterDataRequest.Parser.ParseFrom(data);
            var result = await _mediator.Send(new GetCustomerMasterDataQuery(Guid.Parse(request.Processid))).ConfigureAwait(false);

            var response = new CustomerMasterDataResponse
            {
                Error = result.Error,
                MasterData = new Energinet.DataHub.EnergySupplying.RequestResponse.Requests.CustomerMasterData
                {
                    CustomerId = result.Data?.CustomerId,
                    CustomerName = result.Data?.CustomerName,
                    ElectricalHeatingEffectiveDate = result.Data?.ElectricalHeatingEffectiveDate?
                        .ToTimestamp(),
                    RegisteredByProcessId = result.Data?.RegisteredByProcessId.ToString(),
                    AccountingPointNumber = result.Data?.AccountingPointNumber,
                    SupplyStart = result.Data?.SupplyStart.ToTimestamp(),
                },
            };

            await RespondAsync(response, correlationId).ConfigureAwait(false);

            _logger.LogInformation($"Received request for customer master data: {data}");
        }

        private static string ParseCorrelationIdFromMessage(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("CorrelationId", out var correlationIdValue);
            if (correlationIdValue is string correlationId)
            {
                return correlationId;
            }

            throw new InvalidOperationException("Correlation id is not set on customer master data request message.");
        }

        private async Task RespondAsync(CustomerMasterDataResponse response, string correlationId)
        {
            ServiceBusMessage serviceBusMessage = new(response.ToByteArray())
            {
                ContentType = "application/json",
            };
            serviceBusMessage.CorrelationId = correlationId;
            await _serviceBusSender.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);
        }
    }
}
