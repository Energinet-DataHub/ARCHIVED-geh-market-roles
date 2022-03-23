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
using B2B.CimMessageAdapter;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.Core.App.Common.Abstractions.Identity;
using Energinet.DataHub.Core.App.Common.Abstractions.Security;
using Energinet.DataHub.Core.App.Common.Identity;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.Core.App.FunctionApp.Middleware;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using MarketRoles.B2B.CimMessageAdapter.IntegrationTests.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.MessageReceiver
{
    public static class Program
    {
        public static Task Main()
        {
            var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID") ?? throw new InvalidOperationException(
                "B2C tenant id not found.");
            var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID") ?? throw new InvalidOperationException(
                "Backend service app id not found.");
            var metaDataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    worker.UseMiddleware<RequestResponseLoggingMiddleware>();
                    worker.UseMiddleware<JwtTokenMiddleware>();
                    worker.UseMiddleware<ActorMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    services.AddHttpClient<MarketRolesHttpTrigger>();
                    services.AddScoped<ICorrelationContext>();
                    services.AddScoped<TransactionIdsStub>();
                    services.AddScoped<MessageIdsStub>();
                    services.AddScoped<MarketActivityRecordForwarderStub>();
                    services.AddScoped<SchemaProvider>();
                    services.AddSingleton<IRequestResponseLogging>(s =>
                        {
                            var logger = services.BuildServiceProvider().GetService<ILogger<RequestResponseLoggingBlobStorage>>();
                            var storage = new RequestResponseLoggingBlobStorage(
                                Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONNECTION_STRING") ?? throw new InvalidOperationException(),
                                Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONTAINER_NAME") ?? throw new InvalidOperationException(),
                                logger ?? throw new InvalidOperationException());
                            return storage;
                        });
                    services.AddScoped<RequestResponseLoggingMiddleware>();
                    services.AddScoped<JwtTokenMiddleware>();
                    services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
                    services.AddScoped<IClaimsPrincipalAccessor, ClaimsPrincipalAccessor>();
                    services.AddScoped<ClaimsPrincipalContext>();
                    services.AddScoped(s => new OpenIdSettings(metaDataAddress, audience));
                    services.AddScoped<ActorMiddleware>();
                    services.AddScoped<IActorContext, ActorContext>();
                    services.AddScoped<IActorProvider, ActorProvider>();
                })
                .Build();

            return host.RunAsync();
        }
    }
}
