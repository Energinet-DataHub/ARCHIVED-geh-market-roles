namespace B2B.Transactions.Infrastructure.Authentication.Errors
{
    public class NoAuthenticationHeaderSet : AuthenticationError
    {
        public NoAuthenticationHeaderSet()
        : base("No authorization header is set.")
        {
        }
    }
}
