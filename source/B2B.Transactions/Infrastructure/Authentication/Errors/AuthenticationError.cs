namespace B2B.Transactions.Infrastructure.Authentication.Errors
{
    public abstract class AuthenticationError
    {
        protected AuthenticationError(string message)
        {
            Message = message;
        }

        protected string Message { get; set; }
    }
}
