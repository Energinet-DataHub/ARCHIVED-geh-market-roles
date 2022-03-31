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
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.Authentication
{
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ClaimsPrincipalParser _claimsPrincipalParser;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(ClaimsPrincipalParser claimsPrincipalParser, ILogger<AuthenticationMiddleware> logger)
        {
            _claimsPrincipalParser = claimsPrincipalParser ?? throw new ArgumentNullException(nameof(claimsPrincipalParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            var httpRequestData = context.GetHttpRequestData();
            if (httpRequestData == null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var result = _claimsPrincipalParser.ParseFrom(httpRequestData.Headers);
            if (result.Success == false)
            {
                _logger.LogError(result.Error?.Message);
                var httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.Unauthorized);
                context.SetHttpResponseData(httpResponseData);
                return;
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
