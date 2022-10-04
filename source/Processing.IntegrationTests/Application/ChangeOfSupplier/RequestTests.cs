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
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Processing.Application.ChangeOfSupplier;
using Processing.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Processing.IntegrationTests.Application.ChangeOfSupplier
{
    [IntegrationTest]
    public sealed class RequestTests : TestBase
    {
        public RequestTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Request_WhenMeteringPointDoesNotExist_IsRejected()
        {
            var request = CreateRequest();

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            //TODO: Generation of accept/reject message has been moved to messaging layer - We will handle these test when a full implementation of this process is due
            //AssertOutboxMessage<MessageHubEnvelope>(envelope => envelope.MessageType == DocumentType.RejectChangeOfSupplier);
        }

        [Fact]
        public async Task Request_WhenEnergySupplierIsUnknown_IsRejected()
        {
            CreateAccountingPoint();

            var request = CreateRequest();

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            //TODO: Generation of accept/reject message has been moved to messaging layer - We will handle these test when a full implementation of this process is due
            //AssertOutboxMessage<MessageHubEnvelope>(envelope => envelope.MessageType == DocumentType.RejectChangeOfSupplier);
        }

        [Fact]
        public async Task Request_WhenInputValidationsAreBroken_IsRejected()
        {
            var request = CreateRequest(
                SampleData.GlnNumber,
                SampleData.CustomerNumber,
                "THIS_IS_NOT_VALID_GSRN_NUMBER",
                SampleData.MoveInDate);

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            //TODO: Generation of accept/reject message has been moved to messaging layer - We will handle these test when a full implementation of this process is due
            //AssertOutboxMessage<MessageHubEnvelope>(envelope => envelope.MessageType == DocumentType.RejectChangeOfSupplier);
        }

        private static RequestChangeOfSupplier CreateRequest(string energySupplierGln, string consumerId, string gsrnNumber, string startDate)
        {
            return new RequestChangeOfSupplier(
                EnergySupplierGlnNumber: energySupplierGln,
                SocialSecurityNumber: consumerId,
                AccountingPointGsrnNumber: gsrnNumber,
                StartDate: startDate);
        }

        private static RequestChangeOfSupplier CreateRequest()
        {
            return CreateRequest(
                SampleData.GlnNumber,
                SampleData.CustomerNumber,
                SampleData.GsrnNumber,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(1)).ToString());
        }
    }
}
