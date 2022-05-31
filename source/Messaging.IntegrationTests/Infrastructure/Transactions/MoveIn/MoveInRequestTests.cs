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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Transactions.MoveIn;

public class MoveInRequestTests : TestBase
{
    private readonly HttpClientMock _httpClientMock;
    private readonly MoveInRequestAdapter _requestService;

    public MoveInRequestTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _httpClientMock = new HttpClientMock();
        _requestService = new MoveInRequestAdapter(new Uri("https://someuri"), _httpClientMock, GetService<ISerializer>(), new LoggerDummy<MoveInRequestAdapter>());
    }

    [Fact]
    public async Task Request_is_send_to_processing()
    {
        var request = CreateRequest();

        await _requestService.InvokeAsync(request).ConfigureAwait(false);

        _httpClientMock
            .AssertJsonContent(request);
    }

    [Fact]
    public async Task Throw_when_business_processing_request_is_unsuccessful()
    {
        _httpClientMock.RespondWith(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<HttpRequestException>(() => _requestService.InvokeAsync(CreateRequest()));
    }

    private static MoveInRequest CreateRequest()
    {
        return new MoveInRequest(
            "Consumer1",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            "CPR");
    }
}

#pragma warning disable
public class HttpClientMock : IHttpClientAdapter
{
    private string _messageBody;
    private HttpStatusCode _responseCode = HttpStatusCode.OK;

    public void AssertJsonContent(object expectedContent)
    {
        Assert.True(JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(expectedContent)), JToken.Parse(_messageBody)));
    }

    public async Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content)
    {
        _messageBody = await content.ReadAsStringAsync();
        var response = CreateResponseFromProcessing();
        return response;

    }

    private HttpResponseMessage CreateResponseFromProcessing()
    {
        var businessProcessResponse = new BusinessProcessResponse(new List<string>());
        var content = new StringContent(JsonConvert.SerializeObject(businessProcessResponse));
        var response = new HttpResponseMessage(_responseCode);
        response.Content = content;
        return response;
    }

    public void RespondWith(HttpStatusCode responseCode)
    {
        _responseCode = responseCode;
    }
}

public class LoggerDummy<T> : ILogger<T>
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }
}
