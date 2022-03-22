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
using System.IO;
using System.Net;
using System.Threading.Tasks;
using B2B.CimMessageAdapter;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using MarketRoles.B2B.CimMessageAdapter.IntegrationTests.Stubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace B2B.Transactions.MessageReceiver
{
    public class MarketRolesHttpTrigger
    {
        private readonly TransactionIdsStub _transactionIdsStub = new();
        private readonly MessageIdsStub _messageIdsStub = new();
        private readonly MarketActivityRecordForwarderStub _marketActivityRecordForwarderSpy = new();

        private readonly ICorrelationContext _correlationContext;

        public MarketRolesHttpTrigger(ICorrelationContext correlationContext)
        {
            _correlationContext = correlationContext;
        }

        [Function("MarketRoles")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request, ILogger logger)
        {
            logger.LogInformation("Received MarketRoles request");

            if (request == null) throw new ArgumentNullException(nameof(request));

            var messageReceiver = new CimMessageAdapter.MessageReceiver(_messageIdsStub, _marketActivityRecordForwarderSpy, _transactionIdsStub, new SchemaProviderStub());
            var result = await messageReceiver.ReceiveAsync(request.Body, "requestchangeofsupplier", "1.0").ConfigureAwait(false);

            if (result == Result.Succeeded())
            {
                return CreateResponse(request, HttpStatusCode.Accepted);
            }

            return CreateResponse(request, HttpStatusCode.BadRequest);
        }

        private static HttpResponseData CreateResponse(HttpRequestData request, HttpStatusCode statusCode)
        {
            var response = request.CreateResponse(statusCode);

            return response;
        }
    }
}
