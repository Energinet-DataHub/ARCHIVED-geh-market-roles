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
using System.Security.Claims;
using B2B.CimMessageAdapter.Errors;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace B2B.Transactions.Tests.Infrastructure
{
    #pragma warning disable
    public class AuthenticationTests
    {
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
        public void Authorization_header_must_start_with_bearer()
        {
            var httpRequest = CreateRequestWithAuthorizationHeader("Nobearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6ImpTMVhvMU9XRGpfNTJ2YndHTmd2UU8yVnpNYyJ9.eyJhdWQiOiJjN2U1ZGM1Yy0yZWUwLTQyMGMtYjVkMi01ODZlNzUyNzMwMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwL3YyLjAiLCJpYXQiOjE2NDY5MTU5MTUsIm5iZiI6MTY0NjkxNTkxNSwiZXhwIjoxNjQ2OTE5ODE1LCJhaW8iOiJFMlpnWUVpTEtqSGFNMldOMUxiSmUxdzhNazV4QVFBPSIsImF6cCI6ImE5ODJkZTFmLTM3MDMtNGUzYy1iZTU2LTRlNTQ0MThhMmE1OSIsImF6cGFjciI6IjEiLCJvaWQiOiI5OGZmOGMyMS0xZGMzLTQwYTctYmNmZS02N2UwOTBlN2ZlOTMiLCJyaCI6IjAuQVNJQTZoRjBTbkdzWTB1V1I3aTlURm9nNEZ6YzVjZmdMZ3hDdGRKWWJuVW5NQ3drQUFBLiIsInJvbGVzIjpbImJhbGFuY2VyZXNwb25zaWJsZXBhcnR5IiwiZWxlY3RyaWNhbHN1cHBsaWVyIiwiZ3JpZG9wZXJhdG9yIiwibWV0ZXJkYXRhcmVzcG9uc2libGUiXSwic3ViIjoiOThmZjhjMjEtMWRjMy00MGE3LWJjZmUtNjdlMDkwZTdmZTkzIiwidGlkIjoiNGE3NDExZWEtYWM3MS00YjYzLTk2NDctYjhiZDRjNWEyMGUwIiwidXRpIjoiVEx1UVVKZG43RWU0aFBUQWZiZXdBQSIsInZlciI6IjIuMCJ9.hy8RxBV_LKl7-bt_9cUr9hlVoQ7gicA04I8AXYO02kvdfw0ugBGnimFGZ4rin1PmjKMceigPzN7H49S80z42YI3WUWNEsYX2D0lRWHHhFOd53Yjcu0nL9xQtCZ8Cy4NpD86jxGQvD1pw227TyKL0cpB04tQ1X9CRwlty7qDTZK1Aqa3QYcbR7BKn_gU4N01sX2SZpi-MYOQqeiHpwmHWfwesiVfpumYw6x5g45zObCjyFEGbNIPDOwo3YlBBdGoRSQeaqYwoQMd2lrxOAhGBve_6uCJa8SuFAz36AUewIsQi2EIGpbKmmddwGTWE62owBVtYeEvljovjaBisJK4ZEQ");

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType(typeof(AuthenticationHeaderIsNotBearerToken), result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        private Result Parse(HttpRequestMessage httpRequest)
        {
            var principalParser = new ClaimsPrincipalParser();
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

    public class AuthenticationHeaderIsNotBearerToken : AuthenticationError
    {
        public AuthenticationHeaderIsNotBearerToken()
        {
            Message = "The value defined in authorization header is not start with 'bearer'.";
        }
    }

    public class NoAuthenticationHeaderSet : AuthenticationError
    {
        public NoAuthenticationHeaderSet()
        {
            Message = "No authorization header is set.";
        }
    }

    public abstract class AuthenticationError
    {
        public string Message { get; protected set; }
    }

    public class ClaimsPrincipalParser
    {
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
            var token = authorizationHeaderValue.Substring(7);
            var tokenReader = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters()
            {
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuer = false,
                SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
            };
            var principal = tokenReader.ValidateToken(token, validationParameters, out _);
            return new Result(principal);
        }

        private static bool IsBearer(string? authorizationHeaderValue)
        {
            return authorizationHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class Result
    {
        public Result()
        {
            Success = false;
        }

        private Result(AuthenticationError error)
        {
            Success = false;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        public Result(ClaimsPrincipal claimsPrincipal)
        {
            Success = true;
            ClaimsPrincipal = claimsPrincipal;
        }

        public bool Success { get; }
        public ClaimsPrincipal ClaimsPrincipal { get; }
        public AuthenticationError Error { get; init; }

        public static Result Failed(AuthenticationError error)
        {
            return new Result(error);
        }
    }
    // public class Middleware : IFunctionsWorkerMiddleware
    // {
    //     public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    //     {
    //         var httpRequestData = context.GetHttpRequestData();
    //         var parser = new ClaimsPrincipalParser();
    //         var result = parser.TryParse(httpRequestData.Headers);
    //         return next;
    //     }
    // }
    //
    // public static class Extentions
    // {
    //     public static HttpRequestData? GetHttpRequestData(this FunctionContext functionContext)
    //     {
    //         if (functionContext == null) throw new ArgumentNullException(nameof(functionContext));
    //
    //         var functionBindingsFeature = functionContext.GetIFunctionBindingsFeature();
    //         var type = functionBindingsFeature.GetType();
    //         var inputData = type.GetProperties().Single(p => p.Name == "InputData").GetValue(functionBindingsFeature) as IReadOnlyDictionary<string, object>;
    //         return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
    //     }
    //
    //     private static object GetIFunctionBindingsFeature(this FunctionContext functionContext)
    //     {
    //         var keyValuePair = functionContext.Features.SingleOrDefault(f => f.Key.Name is "IFunctionBindingsFeature");
    //         return keyValuePair.Value;
    //     }
    // }
}
