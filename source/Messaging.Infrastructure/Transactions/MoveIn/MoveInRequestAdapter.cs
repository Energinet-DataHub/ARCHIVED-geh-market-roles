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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Transactions.MoveIn;
public sealed class MoveInRequestAdapter : IMoveInRequestAdapter
{
    private readonly Uri _moveInRequestUrl;
    private readonly ISerializer _serializer;
    private readonly IHttpClientAdapter _httpClientAdapter;
    private readonly ILogger<MoveInRequestAdapter> _logger;

    public MoveInRequestAdapter(Uri moveInRequestUrl, IHttpClientAdapter httpClientAdapter, ISerializer serializer,  ILogger<MoveInRequestAdapter> logger)
    {
        _moveInRequestUrl = moveInRequestUrl;
        _httpClientAdapter = httpClientAdapter;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task<BusinessRequestResult> InvokeAsync(MoveInRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var response = await MoveInAsync(CreateRequestFrom(request)).ConfigureAwait(false);
        return await ParseResultFromAsync(response).ConfigureAwait(false);
    }

    private static MoveInRequestDto CreateRequestFrom(MoveInRequest request)
    {
        return new MoveInRequestDto(
            request.ConsumerName,
            request.EnergySupplierGlnNumber,
            request.AccountingPointGsrnNumber,
            request.StartDate,
            request.TransactionId,
            request.ConsumerId,
            request.ConsumerIdType);
    }

    private async Task<HttpResponseMessage> MoveInAsync(MoveInRequestDto moveInRequestDto)
    {
        using var ms = new MemoryStream();
        await _serializer.SerializeAsync(ms, moveInRequestDto).ConfigureAwait(false);
        ms.Position = 0;
        using var content = new StreamContent(ms);
        var response = await _httpClientAdapter.PostAsync(_moveInRequestUrl, content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<BusinessRequestResult> ParseResultFromAsync(HttpResponseMessage response)
    {
        try
        {
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogInformation($"Response body from business processing: {responseBody}");

            var result = _serializer.Deserialize<BusinessProcessResponse>(responseBody);
            return result.ValidationErrors.Count > 0 ? BusinessRequestResult.Failure(result.ValidationErrors.ToArray()) : BusinessRequestResult.Succeeded();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize response from business processing.");
            throw;
        }
    }
}

public record MoveInRequestDto(
    string? ConsumerName,
    string? EnergySupplierGlnNumber,
    string AccountingPointGsrnNumber,
    string StartDate,
    string TransactionId,
    string? ConsumerId,
    string? ConsumerIdType);

public record BusinessProcessResponse(IReadOnlyCollection<string> ValidationErrors);
