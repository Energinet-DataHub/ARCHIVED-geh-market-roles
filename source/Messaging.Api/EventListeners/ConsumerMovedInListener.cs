using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.EventListeners;

public class ConsumerMovedInListener
{
    private readonly ILogger<ConsumerMovedInListener> _logger;

    public ConsumerMovedInListener(ILogger<ConsumerMovedInListener> logger)
    {
        _logger = logger;
    }

    [Function("ConsumerMovedInListener")]
    public void Run(
        [ServiceBusTrigger("consumer-moved-in", "consumer-moved-in", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS")] byte[] data,
        FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var consumerMovedIn = Contracts.IntegrationEvents.ConsumerMovedIn.Parser.ParseFrom(data);
        _logger.LogInformation($"Received consumer moved in event: {consumerMovedIn}");
    }
}
