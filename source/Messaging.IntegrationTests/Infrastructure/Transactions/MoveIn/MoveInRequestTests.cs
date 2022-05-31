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
using System.Net.Http;
using System.Threading.Tasks;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Transactions.MoveIn;

public class MoveInRequestTests : TestBase
{
    public MoveInRequestTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Request_is_send_to_processing()
    {
        var httpClientMock = new HttpClientMock();
        var service = new MoveInRequestAdapter(new Uri("https://someuri"), httpClientMock, GetService<ISerializer>(), new LoggerDummy<MoveInRequestAdapter>());
        var request = new MoveInRequest(
            "Consumer1",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            "CPR");

        await service.InvokeAsync(request).ConfigureAwait(false);

        httpClientMock
            .WithContentBody(JsonConvert.SerializeObject(request));
    }

    [Fact]
    public async Task Throw_when_business_processing_request_is_unsuccessful()
    {
        var httpClientMock = new HttpClientMock();
        var service = new MoveInRequestAdapter(new Uri("https://someuri"), httpClientMock, GetService<ISerializer>(), new LoggerDummy<MoveInRequestAdapter>());
        var request = new MoveInRequest(
            "Consumer1",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            "CPR");

        await Assert.ThrowsAsync<HttpRequestException>(() => service.InvokeAsync(request));
    }
}

#pragma warning disable
public class HttpClientMock : IHttpClientAdapter
{
    private readonly string _messageBody;
    public void WithContentBody(string expectedContentBody)
    {
        Assert.Equal(expectedContentBody, _messageBody);
    }

    public Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        return Task.FromResult(response);
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
