using System;
using System.Security.Claims;
using B2B.Transactions.Infrastructure.Authentication.Errors;

namespace B2B.Transactions.Infrastructure.Authentication
{
    public class Result
    {
        private Result(AuthenticationError error)
        {
            Success = false;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        private Result(ClaimsPrincipal claimsPrincipal)
        {
            Success = true;
            ClaimsPrincipal = claimsPrincipal;
        }

        public bool Success { get; }

        public ClaimsPrincipal? ClaimsPrincipal { get; }

        public AuthenticationError? Error { get; }

        public static Result Failed(AuthenticationError error)
        {
            return new Result(error);
        }

        public static Result Succeeded(ClaimsPrincipal principal)
        {
            return new Result(principal);
        }
    }
}
