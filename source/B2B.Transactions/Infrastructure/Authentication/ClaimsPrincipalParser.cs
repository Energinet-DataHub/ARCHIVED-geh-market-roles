using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using B2B.Transactions.Infrastructure.Authentication.Errors;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Transactions.Infrastructure.Authentication
{
    public class ClaimsPrincipalParser
    {
        private readonly TokenValidationParameters _validationParameters;

        public ClaimsPrincipalParser(TokenValidationParameters validationParameters)
        {
            _validationParameters = validationParameters ?? throw new ArgumentNullException(nameof(validationParameters));
        }

        public Result ParseFrom(HttpHeaders requestHeaders)
        {
            if (requestHeaders == null) throw new ArgumentNullException(nameof(requestHeaders));
            if (requestHeaders.TryGetValues("authorization", out var authorizationHeaderValues) == false)
            {
                return Result.Failed(new NoAuthenticationHeaderSet());
            }

            var authorizationHeaderValue = authorizationHeaderValues.FirstOrDefault();
            if (authorizationHeaderValue is null || IsBearer(authorizationHeaderValue) == false)
            {
                return Result.Failed(new AuthenticationHeaderIsNotBearerToken());
            }

            return ExtractPrincipalFrom(ParseBearerToken(authorizationHeaderValue));
        }

        private static string ParseBearerToken(string authorizationHeaderValue)
        {
            if (authorizationHeaderValue == null) throw new ArgumentNullException(nameof(authorizationHeaderValue));
            return authorizationHeaderValue.Substring(7);
        }

        private static bool IsBearer(string authorizationHeaderValue)
        {
            if (authorizationHeaderValue == null) throw new ArgumentNullException(nameof(authorizationHeaderValue));
            return authorizationHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase) && authorizationHeaderValue.Length > 7;
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
    }
}
