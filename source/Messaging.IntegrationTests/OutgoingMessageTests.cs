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

using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Configuration;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using Newtonsoft.Json;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests;

public class OutgoingMessageTests : TestBase
{
    public OutgoingMessageTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Test_query()
    {
        var context = GetService<B2BContext>();

        var confirm = new ConfirmRequestChangeOfSupplierMessage(
            DataHubDetails.IdentificationNumber,
            "12345",
            "E65",
            MarketRole.GridOperator,
            DataHubDetails.IdentificationNumber,
            MarketRole.EnergySupplier,
            new MarketActivityRecord("Fake", "Fake", "Fake"));

        var genericPayload = new Domain.OutgoingMessages.GenericNotification.MarketActivityRecord(
            "Fake",
            "Fake",
            "Fake",
            SystemClock.Instance.GetCurrentInstant());
        var generic = new OutgoingMessage(
            DocumentType.GenericNotification,
            DataHubDetails.IdentificationNumber,
            "12345",
            "E65",
            MarketRole.GridOperator,
            DataHubDetails.IdentificationNumber,
            MarketRole.EnergySupplier,
            JsonConvert.SerializeObject(genericPayload));

        context.OutgoingMessages.Add(generic);
        context.OutgoingMessages.Add(confirm);

        await context.SaveChangesAsync().ConfigureAwait(false);

        var found = context.OutgoingMessages.ToList();

        Assert.True(found.Count == 2);
        Assert.True(WillHandle(found[0]));
        Assert.True(WillHandle(found[1]));
    }

    #pragma warning disable
    private bool WillHandle(OutgoingMessage message)
    {
        return message.DocumentType == DocumentType.GenericNotification ||
               message.DocumentType == DocumentType.ConfirmRequestChangeOfSupplier;
    }
}
