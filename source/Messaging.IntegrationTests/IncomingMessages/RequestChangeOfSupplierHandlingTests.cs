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

using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Messaging.Application.IncomingMessages;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions;
using Messaging.Application.Xml;
using Messaging.Application.Xml.SchemaStore;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;
using MessageHeader = Messaging.Application.OutgoingMessages.MessageHeader;

namespace Messaging.IntegrationTests.IncomingMessages
{
    [IntegrationTest]
    public class RequestChangeOfSupplierHandlingTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly ITransactionRepository _transactionRepository;
        private readonly RequestChangeOfSupplierHandler _requestChangeOfSupplierHandler;

        public RequestChangeOfSupplierHandlingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _transactionRepository =
                GetService<ITransactionRepository>();
            _requestChangeOfSupplierHandler = GetService<RequestChangeOfSupplierHandler>();
        }

        [Fact]
        public async Task Transaction_is_registered()
        {
            var incomingMessage = IncomingMessageBuilder.CreateMessage();

            await _requestChangeOfSupplierHandler.HandleAsync(incomingMessage).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.GetById(incomingMessage.MarketActivityRecord.Id);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Reject_if_business_request_is_invalid()
        {
            var messageBuilder = new IncomingMessageBuilder();
            var incomingMessage = messageBuilder
                .WithProcessType("E03")
                .WithConsumerName(null)
                .Build();

            await _requestChangeOfSupplierHandler.HandleAsync(incomingMessage).ConfigureAwait(false);
            await RequestMessageGeneratedBy(incomingMessage.Id).ConfigureAwait(false);

            await AssertRejectMessage().ConfigureAwait(false);
        }

        private static void AssertHeader(XDocument document)
        {
            Assert.NotEmpty(AssertXmlMessage.GetMessageHeaderValue(document, "mRID")!);
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");
            // AssertXmlMessage.AssertHasHeaderValue(document, "process.processType", header.ProcessType);
            AssertXmlMessage.AssertHasHeaderValue(document, "businessSector.type", "23");
            // AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", header.SenderId);
            // AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", "DDZ");
            // AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", header.ReceiverId);
            // AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", header.ReceiverRole);
            AssertXmlMessage.AssertHasHeaderValue(document, "reason.code", "A02");
        }

        private async Task AssertRejectMessage()
        {
            var messageDispatcher = GetService<IMessageDispatcher>() as MessageDispatcherSpy;
            var schema = await GetService<ISchemaProvider>().GetSchemaAsync("rejectrequestchangeofsupplier", "1.0")
                .ConfigureAwait(false);
            var validationResult = await MessageValidator.ValidateAsync(messageDispatcher!.DispatchedMessage!, schema!);
            Assert.True(validationResult.IsValid);

            var document = XDocument.Load(messageDispatcher!.DispatchedMessage!);
            AssertHeader(document);
        }

        private async Task RequestMessageGeneratedBy(string id)
        {
            var rejectMessage = _outgoingMessageStore.GetByOriginalMessageId(id)!;
            await GetService<MessageRequestHandler>().HandleAsync(new[] { rejectMessage.Id.ToString(), }).ConfigureAwait(false);
        }
    }
}
