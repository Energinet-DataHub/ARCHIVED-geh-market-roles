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
using B2B.Transactions.Configuration;
using B2B.Transactions.DataAccess;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Incoming;
using B2B.Transactions.Xml.Outgoing;
using Xunit;
using Xunit.Categories;

namespace B2B.Transactions.IntegrationTests.Transactions
{
    [IntegrationTest]
    public class TransactionHandlingTests : TestBase
    {
        private static readonly SystemDateTimeProviderStub _dateTimeProvider = new();
        private readonly ICorrelationContext _correlationContext;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IncomingMessageStore _incomingMessageStore;
        private readonly IncomingMessageHandler _incomingMessageHandler;

        public TransactionHandlingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _correlationContext = GetService<ICorrelationContext>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _transactionRepository =
                GetService<ITransactionRepository>();
            _unitOfWork = GetService<IUnitOfWork>();
            _incomingMessageStore = GetService<IncomingMessageStore>();
            _incomingMessageHandler = GetService<IncomingMessageHandler>();
        }

        [Fact]
        public async Task Transaction_is_registered()
        {
            var incomingMessage = TransactionBuilder.CreateTransaction();

            await _incomingMessageHandler.HandleAsync(incomingMessage).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.GetById(incomingMessage.Message.MessageId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Incoming_message_is_stored()
        {
            var incomingMessage = TransactionBuilder.CreateTransaction();

            await _incomingMessageHandler.HandleAsync(incomingMessage).ConfigureAwait(false);

            Assert.Equal(incomingMessage, _incomingMessageStore.GetById(incomingMessage.Id));
        }

        private static XDocument CreateDocument(string payload)
        {
            return XDocument.Parse(payload);
        }

        private static void AssertMarketActivityRecord(XDocument document, B2BTransaction transaction)
        {
            Assert.NotNull(AssertXmlMessage.GetMarketActivityRecordValue(document, "mRID"));
            AssertXmlMessage.AssertMarketActivityRecordValue(document, "originalTransactionIDReference_MktActivityRecord.mRID", transaction.MarketActivityRecord.Id);
            AssertXmlMessage.AssertMarketActivityRecordValue(document, "marketEvaluationPoint.mRID", transaction.MarketActivityRecord.MarketEvaluationPointId);
        }

        private static void AssertHeader(XDocument document, B2BTransaction transaction)
        {
            Assert.NotNull(AssertXmlMessage.GetMessageHeaderValue(document, "mRID"));
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");
            AssertXmlMessage.AssertHasHeaderValue(document, "process.processType", transaction.Message.ProcessType);
            AssertXmlMessage.AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", "5790001330552");
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", "DDZ");
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", transaction.Message.SenderId);
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", transaction.Message.SenderRole);
            AssertXmlMessage.AssertHasHeaderValue(document, "createdDateTime", _dateTimeProvider.Now().ToString());
            AssertXmlMessage.AssertHasHeaderValue(document, "reason.code", "A01");
        }
    }
}
