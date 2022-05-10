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
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Processing.Application.ChangeOfSupplier;
using Processing.Infrastructure.Serialization;

namespace Processing.Api;

public class MoveInProcess
{
    private readonly ILogger<MoveInProcess> _logger;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IMediator _mediator;

    public MoveInProcess(
        ILogger<MoveInProcess> logger,
        IJsonSerializer jsonSerializer,
        IMediator mediator)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _mediator = mediator;
    }

    [Function("MoveInProcess")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request)
    {
        _logger.LogInformation($"Received {nameof(MoveInProcess)} request");

        var command = _jsonSerializer.Deserialize<RequestChangeOfSupplier>(request?.Body.ToString() ?? throw new InvalidOperationException());
        var responseBody = _jsonSerializer.Serialize(await _mediator.Send(command).ConfigureAwait(false));
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Body = new MemoryStream(Encoding.UTF8.GetBytes(responseBody));
        return await Task.FromResult(response).ConfigureAwait(false);
    }
}
