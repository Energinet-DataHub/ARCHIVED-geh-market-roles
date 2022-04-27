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
// limitations under the License.using System;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using B2B.Transactions.Api.Middleware.Authentication.Bearer;
using B2B.Transactions.Api.Middleware.Authentication.MarketActors;
using B2B.Transactions.Api.Middleware.Correlation;
using B2B.Transactions.Api.Middleware.ServiceBus;
using B2B.Transactions.Infrastructure.Configuration;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Transactions.Api
{
    public static class Program
    {
        public static async Task Main()
        {
            var runtime = RuntimeEnvironment.Default;
            var tokenValidationParameters = await GetTokenValidationParametersAsync(runtime).ConfigureAwait(false);

            var host = ConfigureHost(tokenValidationParameters, runtime);

            await host.RunAsync().ConfigureAwait(false);
        }

        public static TokenValidationParameters DevelopmentTokenValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
            };
        }

        public static IHost ConfigureHost(TokenValidationParameters tokenValidationParameters, RuntimeEnvironment runtime)
        {
            return new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    worker.UseMiddleware<RequestResponseLoggingMiddleware>();
                    worker.UseMiddleware<BearerAuthenticationMiddleware>();
                    worker.UseMiddleware<ClaimsEnrichmentMiddleware>();
                    worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
                    worker.UseMiddleware<ServiceBusMessageMetadataMiddleWare>();
                })
                .ConfigureServices(services =>
                {
                    var databaseConnectionString = runtime.DB_CONNECTION_STRING;
                    CompositionRoot.Initialize(services)
                        .AddBearerAuthentication(tokenValidationParameters)
                        .AddDatabaseConnectionFactory(databaseConnectionString!)
                        .AddSystemClock(new SystemDateTimeProvider())
                        .AddDatabaseContext(databaseConnectionString!)
                        .AddCorrelationContext(sp =>
                        {
                            var correlationContext = new CorrelationContext();
                            if (!runtime.IsRunningLocally()) return correlationContext;
                            correlationContext.SetId(Guid.NewGuid().ToString());
                            correlationContext.SetParentId(Guid.NewGuid().ToString());

                            return correlationContext;
                        })
                        .AddTransactionQueue(
                            runtime.TRANSACTIONS_QUEUE_SENDER_CONNECTION_STRING!,
                            runtime.TRANSACTIONS_QUEUE_NAME!)
                        .AddRequestLogging(
                            runtime.REQUEST_RESPONSE_LOGGING_CONNECTION_STRING!,
                            runtime.REQUEST_RESPONSE_LOGGING_CONTAINER_NAME!);
                })
                .Build();
        }

        private static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(RuntimeEnvironment runtime)
        {
            if (runtime.IsRunningLocally())
            {
                return DevelopmentTokenValidationParameters();
            }

            var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID") ?? throw new InvalidOperationException("B2C tenant id not found.");
            var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID") ?? throw new InvalidOperationException("Backend service app id not found.");
            var metaDataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
            var openIdConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metaDataAddress, new OpenIdConnectConfigurationRetriever());
            var stsConfig = await openIdConfigurationManager.GetConfigurationAsync().ConfigureAwait(false);
            return new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.Zero,
                ValidAudience = audience,
                IssuerSigningKeys = stsConfig.SigningKeys,
                ValidIssuer = stsConfig.Issuer,
            };
        }
    }
}
