using Microsoft.Extensions.Hosting;

namespace B2B.Transactions.CimMessageAdapter.Receiver
{
    public static class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
        }
    }
}
