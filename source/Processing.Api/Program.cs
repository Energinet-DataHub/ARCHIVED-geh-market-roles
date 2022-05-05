using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Processing.Api.Configuration;
using Processing.Infrastructure.Configuration;

namespace Processing.Api
{
    public static class Program
    {
        public static async Task Main()
        {
            var runtime = RuntimeEnvironment.Default;

            var host = ConfigureHost(runtime);

            await host.RunAsync().ConfigureAwait(false);
        }

        private static IHost ConfigureHost(RuntimeEnvironment runtime)
        {
            return new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                })
                .ConfigureServices(services =>
                {
                    CompositionRoot.Initialize(services);
                })
                .Build();
        }
    }
}
