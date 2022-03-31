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

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using B2B.Transactions.Infrastructure.Authentication;
using B2B.Transactions.Infrastructure.Authentication.Bearer;
using B2B.Transactions.Infrastructure.Authentication.Bearer.Errors;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace B2B.Transactions.Tests.Infrastructure
{
    public class JwtTokenParserTests
    {
        private static TokenValidationParameters DisableAllTokenValidations => new()
        {
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuer = false,
            SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
        };

        [Fact]
        public void Returns_failure_when_token_validation_fails()
        {
            var token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ";
            using var httpRequest = CreateRequestWithAuthorizationHeader("bearer " + token);

            var result = Parse(httpRequest, new TokenValidationParameters()
            {
                ValidateLifetime = true,
            });

            Assert.False(result.Success);
            Assert.IsType<TokenValidationFailed>(result.Error);
            Assert.Equal(token, result.Token);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_claims_principal()
        {
            using var httpRequest = CreateRequestWithAuthorizationHeader("bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ");

            var result = Parse(httpRequest);

            Assert.True(result.Success);
            Assert.NotNull(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_failure_when_no_authorization_header_is_set()
        {
            using var httpRequest = CreateRequest();

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType<NoAuthenticationHeaderSet>(result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_failure_when_authorization_header_is_empty()
        {
            using var httpRequest = CreateRequestWithAuthorizationHeader("bearer ");

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType<AuthenticationHeaderIsNotBearerToken>(result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Authorization_header_must_start_with_bearer()
        {
            using var httpRequest = CreateRequestWithAuthorizationHeader("Nobearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ");

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType<AuthenticationHeaderIsNotBearerToken>(result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        private static Result Parse(HttpRequestMessage httpRequest, TokenValidationParameters? validationParameters = null)
        {
            var principalParser = new JwtTokenParser(validationParameters ?? DisableAllTokenValidations);
            return principalParser.ParseFrom(httpRequest.Headers);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private static HttpRequestMessage CreateRequestWithAuthorizationHeader(string value)
        {
            var httpRequest = CreateRequest();
            httpRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(value);
            return httpRequest;
        }
    }
}
