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
using Microsoft.Extensions.Logging;
using Processing.Infrastructure.RequestAdapters;

namespace Processing.Api.MoveIn;

public class MoveInHttpTrigger
{
    private readonly ILogger<MoveInHttpTrigger> _logger;
    private readonly JsonMoveInAdapter _adapter;

    public MoveInHttpTrigger(
        ILogger<MoveInHttpTrigger> logger,
        JsonMoveInAdapter adapter)
    {
        _logger = logger;
        _adapter = adapter;
    }

    [Function("MoveIn")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        _logger.LogInformation($"Received move in request");
        var result = await _adapter.ReceiveAsync(request.Body).ConfigureAwait(false);
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Body = result.Content;
        return response;
    }
}
