namespace B2B.Transactions.Infrastructure.Authentication.Errors
{
    public class AuthenticationHeaderIsNotBearerToken : AuthenticationError
    {
        public AuthenticationHeaderIsNotBearerToken()
        : base("The value defined in authorization header is not start with 'bearer'.")
        {
        }
    }
}
