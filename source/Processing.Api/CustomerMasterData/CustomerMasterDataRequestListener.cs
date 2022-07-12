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
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Processing.Api.Configuration;
using Processing.Application.Customers.GetCustomerMasterData;
using Processing.Infrastructure.Configuration.Serialization;

namespace Processing.Api.CustomerMasterData
{
    public class CustomerMasterDataRequestListener
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly ServiceBusSender _serviceBusSender;
        private readonly IJsonSerializer _jsonSerializer;

        public CustomerMasterDataRequestListener(
            ILogger logger,
            IMediator mediator,
            ServiceBusSender serviceBusSender,
            IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _mediator = mediator;
            _serviceBusSender = serviceBusSender;
            _jsonSerializer = jsonSerializer;
        }

        [Function("CustomerMasterDataRequestListener")]
        public async Task RunAsync(
            [ServiceBusTrigger("%CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME%", Connection = "MARKET_ROLES_SERVICE_BUS_LISTEN_CONNECTION_STRING")] string data,
            FunctionContext context)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var customerMasterDatayQuery = _jsonSerializer.Deserialize<GetCustomerMasterDataQuery>(data);
            var result = await _mediator.Send(customerMasterDatayQuery).ConfigureAwait(false);
            var resultAsJsonString = _jsonSerializer.Serialize(result);

            ServiceBusMessage serviceBusMessage = new(resultAsJsonString)
            {
                ContentType = "application/json",
            };
            await _serviceBusSender.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);

            _logger.LogInformation($"Received request for customer master data: {data}");
        }
    }
}
