namespace B2B.Transactions.Infrastructure.Authentication.Errors
{
    public class TokenValidationFailed : AuthenticationError
    {
        public TokenValidationFailed(string message)
         : base(message)
        {
        }
    }
}
