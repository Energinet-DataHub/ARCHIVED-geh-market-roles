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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using B2B.Transactions.Infrastructure.Authentication;
using B2B.Transactions.Infrastructure.Authentication.Errors;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace B2B.Transactions.Tests.Infrastructure
{
    #pragma warning disable
    public class AuthenticationTests
    {
        [Fact]
        public void Returns_failure_when_token_validation_fails()
        {
            var httpRequest = CreateRequestWithAuthorizationHeader("bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ");

            var result = Parse(httpRequest, new TokenValidationParameters()
            {
                ValidateLifetime = true,
            });

            Assert.False(result.Success);
            Assert.IsType(typeof(TokenValidationFailed), result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_claims_principal()
        {
            var httpRequest = CreateRequestWithAuthorizationHeader("bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ");

            var result = Parse(httpRequest);

            Assert.True(result.Success);
            Assert.NotNull(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_failure_when_no_authorization_header_is_set()
        {
            var httpRequest = CreateRequest();

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType(typeof(NoAuthenticationHeaderSet), result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_failure_when_authorization_header_is_empty()
        {
            var httpRequest = CreateRequestWithAuthorizationHeader("bearer ");

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType(typeof(AuthenticationHeaderIsNotBearerToken), result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Authorization_header_must_start_with_bearer()
        {
            var httpRequest = CreateRequestWithAuthorizationHeader("Nobearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ");

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType(typeof(AuthenticationHeaderIsNotBearerToken), result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        private TokenValidationParameters DisableAllValidations => new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuer = false,
            SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
        };

        private Result Parse(HttpRequestMessage httpRequest, TokenValidationParameters? validationParameters = null)
        {
            var principalParser = new ClaimsPrincipalParser(validationParameters ?? DisableAllValidations);
            return principalParser.TryParse(httpRequest.Headers);
        }

        private HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private HttpRequestMessage CreateRequestWithAuthorizationHeader(string value)
        {
            var httpRequest = CreateRequest();
            httpRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(value);
            return httpRequest;
        }
    }

    public class ClaimsPrincipalParser
    {
        private readonly TokenValidationParameters _validationParameters;

        public ClaimsPrincipalParser(TokenValidationParameters validationParameters)
        {
            _validationParameters = validationParameters ?? throw new ArgumentNullException(nameof(validationParameters));
        }

        public Result TryParse(HttpHeaders requestHeaders)
        {
            if (requestHeaders.TryGetValues("authorization", out var authorizationHeaderValues) == false)
            {
                return Result.Failed(new NoAuthenticationHeaderSet());
            }

            var authorizationHeaderValue = authorizationHeaderValues.FirstOrDefault();

            if (IsBearer(authorizationHeaderValue) == false)
            {
                return Result.Failed(new AuthenticationHeaderIsNotBearerToken());
            }

            return ExtractPrincipalFrom(ParseBearerToken(authorizationHeaderValue));
        }

        private Result ExtractPrincipalFrom(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, _validationParameters, out _);
                return Result.Succeeded(principal);
            }
            catch (SecurityTokenException e)
            {
                return Result.Failed(new TokenValidationFailed(e.Message));
            }
        }

        private static string ParseBearerToken(string? authorizationHeaderValue)
        {
            return authorizationHeaderValue.Substring(7);
        }

        private static bool IsBearer(string? authorizationHeaderValue)
        {
            return authorizationHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase) && authorizationHeaderValue.Length > 7;
        }
    }
}
