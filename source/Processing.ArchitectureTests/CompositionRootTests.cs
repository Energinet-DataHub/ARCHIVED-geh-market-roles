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
        program.ConfigureApplication();

        program.AssertConfiguration();
    }
}
