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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Processing.Api.MoveIn;

public class UpdateCustomerMasterDataRequestListener
{
    private readonly ILogger<UpdateCustomerMasterDataRequestListener> _logger;

    public UpdateCustomerMasterDataRequestListener(ILogger<UpdateCustomerMasterDataRequestListener> logger)
    {
        _logger = logger;
    }

    [Function("UpdateCustomerMasterDataRequestListener")]
    public void Run([ServiceBusTrigger("%CUSTOMER_MASTER_DATA_UPDATE_REQUEST_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data, FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogInformation($"Received request to update customer data");
    }
}
