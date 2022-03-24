using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using B2B.CimMessageAdapter;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Schema;
using B2B.CimMessageAdapter.Transactions;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.Core.App.Common.Abstractions.Identity;
using Energinet.DataHub.Core.App.Common.Abstractions.Security;
using Energinet.DataHub.Core.App.Common.Identity;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.CimMessageAdapter.Receiver
{
    public static class Program
    {
        public static Task Main()
        {
            var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID") ?? throw new InvalidOperationException(
                "B2C tenant id not found.");
            var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID") ?? throw new InvalidOperationException(
                "Backend service app id not found.");
            var metaDataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    worker.UseMiddleware<RequestResponseLoggingMiddleware>();
                    //worker.UseMiddleware<JwtTokenMiddleware>();
                    //worker.UseMiddleware<ActorMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IJsonSerializer, JsonSerializer>();
                    services.AddScoped<SchemaStore>();
                    services.AddScoped<ISchemaProvider, SchemaProvider>();
                    services.AddScoped<MessageReceiver>();

                    services.AddScoped<ICorrelationContext>(_ =>
                    {
                        var context = new CorrelationContext();
                        context.SetId(Guid.NewGuid().ToString());
                        context.SetParentId(Guid.NewGuid().ToString());
                        return context;
                    });
                    services.AddScoped<ITransactionIds, TransactionIdRegistry>();
                    services.AddScoped<IMessageIds, MessageIdRegistry>();
                    services.AddSingleton<ServiceBusSender>(serviceProvider =>
                    {
                        var connectionString = Environment.GetEnvironmentVariable("TRANSACTION_QUEUE_CONNECTION_STRING");
                        var topicName = Environment.GetEnvironmentVariable("TRANSACTION_QUEUE_NAME");
                        return new ServiceBusClient(connectionString).CreateSender(topicName);
                    });
                    services.AddScoped<ITransactionQueueDispatcher, TransactionQueueDispatcher>();
                    services.AddLogging();
                    services.AddSingleton(typeof(ILogger), typeof(Logger<B2BCimHttpTrigger>));

                    services.AddSingleton<IRequestResponseLogging>(s =>
                        {
                            var logger = services.BuildServiceProvider().GetService<ILogger<RequestResponseLoggingBlobStorage>>();
                            var storage = new RequestResponseLoggingBlobStorage(
                                Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONNECTION_STRING") ?? throw new InvalidOperationException(),
                                Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONTAINER_NAME") ?? throw new InvalidOperationException(),
                                logger ?? throw new InvalidOperationException());
                            return storage;
                        });
                    services.AddScoped<RequestResponseLoggingMiddleware>();

                    services.AddScoped<IClaimsPrincipalAccessor, ClaimsPrincipalAccessor>();
                    services.AddScoped<ClaimsPrincipalContext>();
                    services.AddScoped(s => new OpenIdSettings(metaDataAddress, audience));
                    services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
                    //services.AddScoped<IActorContext, ActorContext>();
                    services.AddScoped<IActorContext>(sp =>
                    {
                        var context = new ActorContext();
                        context.CurrentActor = new Actor(Guid.NewGuid(), "GLN", "5799999933318", string.Empty);
                        return context;
                    });
                    services.AddScoped<IActorProvider, ActorProvider>();
                    services.AddScoped<IDbConnectionFactory>(_ =>
                    {
                        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
                        if (connectionString is null)
                        {
                            throw new ArgumentNullException(nameof(connectionString));
                        }

                        return new SqlDbConnectionFactory(connectionString);
                    });
                })
                .Build();

            return host.RunAsync();
        }
    }
}
