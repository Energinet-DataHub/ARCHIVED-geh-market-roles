// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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
        Environment.SetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND", "Endpoint=sb://somespace.windows.net/;SharedAccessKeyName=somekey;SharedAccessKey=somevalue");
        Environment.SetEnvironmentVariable("CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME", "somevalue");
        program.ConfigureApplication();

        program.AssertConfiguration();
    }
}
