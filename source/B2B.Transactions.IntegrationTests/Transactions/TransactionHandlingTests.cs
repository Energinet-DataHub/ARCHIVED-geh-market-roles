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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using B2B.Transactions.Configuration;
using B2B.Transactions.DataAccess;
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
        private IMessageFactory<IDocument> _messageFactory = new AcceptMessageFactory(_dateTimeProvider, new MessageValidator(new SchemaProvider(new SchemaStore())));

        public TransactionHandlingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _correlationContext = GetService<ICorrelationContext>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _transactionRepository =
                GetService<ITransactionRepository>();
            _unitOfWork = GetService<IUnitOfWork>();
        }

        [Fact]
        public void Transaction_is_registered()
        {
            var incomingMessageStore = new IncomingMessageStore();
            var incomingMessage = TransactionBuilder.CreateTransaction();
            var incomingMessageHandler = new IncomingMessageHandler(incomingMessageStore, _transactionRepository, _outgoingMessageStore, _unitOfWork, _correlationContext);

            incomingMessageHandler.HandleAsync(incomingMessage);

            var savedTransaction = _transactionRepository.GetById(incomingMessage.Message.MessageId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Message_is_generated_when_transaction_is_accepted()
        {
            var now = _dateTimeProvider.Now();
            _dateTimeProvider.SetNow(now);
            var transaction = TransactionBuilder.CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var acceptMessage = _outgoingMessageStore.GetUnpublished().FirstOrDefault();
            Assert.NotNull(acceptMessage);
            var document = CreateDocument(acceptMessage!.MessagePayload ?? string.Empty);

            AssertHeader(document, transaction);
            AssertMarketActivityRecord(document, transaction);
        }

        [Fact]
        public void Incoming_message_is_stored()
        {
            var incomingMessageStore = new IncomingMessageStore();
            var incomingMessage = TransactionBuilder.CreateTransaction();
            var incomingMessageHandler = new IncomingMessageHandler(incomingMessageStore, _transactionRepository, _outgoingMessageStore, _unitOfWork, _correlationContext);

            incomingMessageHandler.HandleAsync(incomingMessage);

            Assert.Equal(incomingMessage, incomingMessageStore.GetById(incomingMessage.Id));
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

        private Task RegisterTransaction(B2BTransaction transaction)
        {
            var handler = new RegisterTransaction(_outgoingMessageStore, _transactionRepository, _messageFactory, _unitOfWork, _correlationContext);
            return handler.HandleAsync(transaction);
        }
    }

#pragma warning disable
    public class IncomingMessageStore
    {
        private List<B2BTransaction> messages = new();

        public B2BTransaction? GetById(string incomingMessageId)
        {
            return messages.Find(x => x.Id == incomingMessageId);
        }

        public void Add(B2BTransaction incomingMessage)
        {
            messages.Add(incomingMessage);
        }
    }

    public class IncomingMessageHandler
    {
        private readonly IncomingMessageStore _store;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICorrelationContext _correlationContext;

        public IncomingMessageHandler(IncomingMessageStore store, ITransactionRepository transactionRepository, IOutgoingMessageStore outgoingMessageStore, IUnitOfWork unitOfWork, ICorrelationContext correlationContext)
        {
            _store = store;
            _transactionRepository = transactionRepository;
            _outgoingMessageStore = outgoingMessageStore;
            _unitOfWork = unitOfWork;
            _correlationContext = correlationContext;
        }

        public void HandleAsync(B2BTransaction incomingMessage)
        {
            _store.Add(incomingMessage);
            if (incomingMessage == null) throw new ArgumentNullException(nameof(incomingMessage));
            var acceptedTransaction = new AcceptedTransaction(incomingMessage.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);
            var outgoingMessage = new OutgoingMessage("ConfirmRequestChangeOfSupplier", string.Empty, incomingMessage.Message.ReceiverId, _correlationContext.Id, incomingMessage.MarketActivityRecord.Id, incomingMessage.MarketActivityRecord.MarketEvaluationPointId);

            _outgoingMessageStore.Add(outgoingMessage);

            _unitOfWork.CommitAsync();
        }
    }

    #pragma warning restore
}
