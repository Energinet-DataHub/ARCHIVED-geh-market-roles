using System;
using Processing.Api;
using Xunit;

namespace Processing.ArchitectureTests;

public class CompositionRootTests
{
    [Fact]
    public void Ensure_registrations()
    {
        var program = new Program();
        Environment.SetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING", "SomeString");
        Environment.SetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS", "Endpoint=sb://somespace.windows.net/;SharedAccessKeyName=somekey;SharedAccessKey=somevalue");
        program.ConfigureApplication();

        program.AssertConfiguration();
    }
}
