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
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.Authentication
{
    public class ClaimsEnrichmentMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ActorProvider _actorProvider;
        private readonly CurrentAuthenticatedUser _currentAuthenticatedUser;
        private readonly ILogger<ClaimsEnrichmentMiddleware> _logger;

        public ClaimsEnrichmentMiddleware(ActorProvider actorProvider, CurrentAuthenticatedUser currentAuthenticatedUser, ILogger<ClaimsEnrichmentMiddleware> logger)
        {
            _actorProvider = actorProvider ?? throw new ArgumentNullException(nameof(actorProvider));
            _currentAuthenticatedUser = currentAuthenticatedUser ?? throw new ArgumentNullException(nameof(currentAuthenticatedUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            var httpRequestData = context.GetHttpRequestData();
            if (httpRequestData == null)
            {
                _logger.LogTrace("No HTTP request data was available.");
                await next(context).ConfigureAwait(false);
                return;
            }

            if (_currentAuthenticatedUser.ClaimsPrincipal is null)
            {
                _logger.LogError("No current authenticated user");
                SetUnauthorized(context, httpRequestData);
                return;
            }

            var marketActorId = GetMarketActorId(_currentAuthenticatedUser.ClaimsPrincipal);
            if (string.IsNullOrEmpty(marketActorId))
            {
                _logger.LogError("Could not read market actor id from claims principal.");
                SetUnauthorized(context, httpRequestData);
                return;
            }

            var actor = await _actorProvider.GetActorAsync(Guid.Parse(marketActorId)).ConfigureAwait(false);
            if (actor is null)
            {
                _logger.LogError($"Could not find an actor in the database with id {marketActorId}");
                SetUnauthorized(context, httpRequestData);
                return;
            }

            var identity = CreateClaimsIdentityFrom(actor);
            _currentAuthenticatedUser.SetCurrentUser(new ClaimsPrincipal(identity));

            await next(context).ConfigureAwait(false);
        }

        private static string? GetMarketActorId(ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static void SetUnauthorized(FunctionContext context, HttpRequestData httpRequestData)
        {
            var httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.Unauthorized);
            context.SetHttpResponseData(httpResponseData);
        }

        private ClaimsIdentity CreateClaimsIdentityFrom(Actor actor)
        {
            var claims = _currentAuthenticatedUser.ClaimsPrincipal!.Claims.ToList();
            claims.Add(new Claim("actorid", actor.Identifier));
            claims.Add(new Claim("actoridtype", actor.IdentificationType));

            var currentIdentity = _currentAuthenticatedUser.ClaimsPrincipal?.Identity as ClaimsIdentity;
            var identity = new ClaimsIdentity(
                claims,
                currentIdentity!.AuthenticationType,
                currentIdentity.NameClaimType,
                currentIdentity.RoleClaimType);
            return identity;
        }
    }
}
