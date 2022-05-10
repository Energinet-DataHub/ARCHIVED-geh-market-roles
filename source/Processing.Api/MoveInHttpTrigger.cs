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
using Processing.Api.Dtos;
using Processing.Application.MoveIn;
using Processing.Infrastructure.Configuration.Serialization;

namespace Processing.Api;

public class MoveInHttpTrigger
{
    private readonly ILogger<MoveInHttpTrigger> _logger;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IMediator _mediator;

    public MoveInHttpTrigger(
        ILogger<MoveInHttpTrigger> logger,
        IJsonSerializer jsonSerializer,
        IMediator mediator)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _mediator = mediator;
    }

    [Function("MoveIn")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request)
    {
        _logger.LogInformation($"Received {nameof(MoveInHttpTrigger)} request");

        var dto = _jsonSerializer.Deserialize<MoveInRequestDto>(request?.Body.ToString() ?? throw new InvalidOperationException());
        _logger.LogInformation($"Deserialized into move in request dto with transactionId: {dto.TransactionId}");
        var command = new MoveInRequest(
            new XConsumer(dto.ConsumerName ?? string.Empty),
            dto.TransactionId,
            dto.EnergySupplierGlnNumber ?? string.Empty,
            dto.SocialSecurityNumber ?? string.Empty,
            dto.VATNumber ?? string.Empty,
            dto.AccountingPointGsrnNumber,
            dto.StartDate);

        var businessProcessResult = await _mediator.Send(command).ConfigureAwait(false);

        var responseBodyDto = new ResponseDto();
        foreach (var error in businessProcessResult.ValidationErrors)
        {
            responseBodyDto.ValidationErrors.Add(new ValidationErrorDto(error.Code, error.Message));
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Body = new MemoryStream(Encoding.UTF8.GetBytes(_jsonSerializer.Serialize(responseBodyDto)));
        return response;
    }
}
