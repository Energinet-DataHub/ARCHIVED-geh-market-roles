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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Transactions;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Xunit;

namespace B2B.Transactions.Tests
{
#pragma warning disable
    public class TransactionHandlingTests
    {
        private readonly TransactionRepository _transactionRepository = new();
        private readonly SystemDateTimeProviderStub _dateTimeProvider = new();
        private readonly XNamespace _namespace = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
        private MessageQueue _outgoingMessages = new();

        [Fact]
        public async Task Transaction_is_registered()
        {
            var transaction = CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.Get(transaction.Message.MessageId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public void Accept_message_is_sent_to_sender_when_transaction_is_accepted()
        {
            var now = _dateTimeProvider.Now();
            _dateTimeProvider.SetNow(now);
            var transaction = CreateTransaction();
            RegisterTransaction(transaction);

            var acceptMessage = _outgoingMessages.Messages.FirstOrDefault();
            Assert.NotNull(acceptMessage);
            Assert.NotNull(GetMessageHeaderValue(acceptMessage, "mRID"));
            AssertHasHeaderValue(acceptMessage, "type", "414");

            AssertHasHeaderValue(acceptMessage, "process.processType", transaction.Message.ProcessType);
            AssertHasHeaderValue(acceptMessage, "businessSector.type", "23");
            AssertHasHeaderValue(acceptMessage, "sender_MarketParticipant.mRID", "5790001330552");
            AssertHasHeaderValue(acceptMessage, "sender_MarketParticipant.marketRole.type", "DDZ");
            AssertHasHeaderValue(acceptMessage, "receiver_MarketParticipant.mRID", transaction.Message.SenderId);
            AssertHasHeaderValue(acceptMessage, "receiver_MarketParticipant.marketRole.type", transaction.Message.SenderRole);
            AssertHasHeaderValue(acceptMessage, "createdDateTime", now.ToString());
            AssertHasHeaderValue(acceptMessage, "reason.code", "A01");

            Assert.NotNull(GetMarketActivityRecordValue(acceptMessage, "mRID"));
            AssertMarketActivityRecordValue(acceptMessage, "originalTransactionIDReference_MktActivityRecord.mRID", transaction.MarketActivityRecord.Id);
            AssertMarketActivityRecordValue(acceptMessage, "marketEvaluationPoint.mRID", transaction.MarketActivityRecord.MarketEvaluationPointId);
        }

        private Task RegisterTransaction(B2BTransaction transaction)
        {
            var useCase = new RegisterTransaction(_outgoingMessages, new MessageIdGenerator(),
                new TransactionIdGenerator(), _dateTimeProvider, _transactionRepository);
            return useCase.HandleAsync(transaction);
        }

        private B2BTransaction CreateTransaction()
        {
            return B2BTransaction.Create(
                new MessageHeader("fake", "fake", "fake", "fake", "fake", "somedate", "fake"),
                new MarketActivityRecord()
                {
                    BalanceResponsibleId = "fake",
                    Id = "fake",
                    ConsumerId = "fake",
                    ConsumerName = "fake",
                    EffectiveDate = "fake",
                    EnergySupplierId = "fake",
                    MarketEvaluationPointId = "fake",
                });
        }

        private void AssertHasHeaderValue(AcceptMessage message, string elementName, string expectedValue)
        {
            Assert.Equal(expectedValue, GetMessageHeaderValue(message, elementName));
        }

        private void AssertMarketActivityRecordValue(AcceptMessage message, string elementName, string expectedValue)
        {
            Assert.Equal(expectedValue, GetMarketActivityRecordValue(message, elementName));
        }

        private string GetMarketActivityRecordValue(AcceptMessage message, string elementName)
        {
            return GetHeaderElement(message)?.Element(_namespace + "MktActivityRecord")?.Element(elementName)?.Value;
        }

        private string? GetMessageHeaderValue(AcceptMessage message, string elementName)
        {
            return GetHeaderElement(message)?.Element(elementName)?.Value;
        }

        private XElement GetHeaderElement(AcceptMessage message)
        {
            var document = XDocument.Parse(message.MessagePayload);
            return document?.Element(_namespace + "ConfirmRequestChangeOfSupplier_MarketDocument");
        }
    }

    public class RegisterTransaction
    {
        private readonly MessageQueue _messageQueue;
        private readonly MessageIdGenerator _messageIdGenerator;
        private readonly TransactionIdGenerator _transactionIdGenerator;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly TransactionRepository _transactionRepository;

        public RegisterTransaction(MessageQueue messageQueue, MessageIdGenerator messageIdGenerator, TransactionIdGenerator transactionIdGenerator, ISystemDateTimeProvider systemDateTimeProvider, TransactionRepository transactionRepository)
        {
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
            _messageIdGenerator = messageIdGenerator ?? throw new ArgumentNullException(nameof(messageIdGenerator));
            _transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        }

        public Task HandleAsync(B2BTransaction transaction)
        {
            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);

            var settings = new XmlWriterSettings() { OmitXmlDeclaration = false, Encoding = Encoding.UTF8};

            using var output = new Utf8StringWriter();
            using var writer = XmlWriter.Create(output, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("cim", "ConfirmRequestChangeOfSupplier_MarketDocument", "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd");
            writer.WriteElementString("mRID", null, GenerateMessageId());
            writer.WriteElementString("type", null, "414");
            writer.WriteElementString("process.processType", null, transaction.Message.ProcessType);
            writer.WriteElementString("businessSector.type", null, "23");

            writer.WriteStartElement("sender_MarketParticipant.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue("5790001330552");
            writer.WriteEndElement();

            writer.WriteElementString("sender_MarketParticipant.marketRole.type", null, "DDZ");

            writer.WriteStartElement("receiver_MarketParticipant.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.Message.SenderId);
            writer.WriteEndElement();
            writer.WriteElementString("receiver_MarketParticipant.marketRole.type", null, transaction.Message.SenderRole);
            writer.WriteElementString("createdDateTime", null, GetCurrentDateTime());
            writer.WriteElementString("reason.code", null, "A01");

            writer.WriteStartElement("cim", "MktActivityRecord", null);
            writer.WriteElementString("mRID", null, GenerateTransactionId());
            writer.WriteElementString("originalTransactionIDReference_MktActivityRecord.mRID", null, transaction.MarketActivityRecord.Id);

            writer.WriteStartElement("marketEvaluationPoint.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.MarketActivityRecord.EnergySupplierId);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Close();
            output.Flush();

            _messageQueue.Add(new AcceptMessage()
            {
                MessagePayload = output.ToString(),
            });

            return Task.CompletedTask;
        }

        private string GetCurrentDateTime()
        {
            return _systemDateTimeProvider.Now().ToString();
        }

        private string GenerateTransactionId()
        {
            return _transactionIdGenerator.Generate();
        }

        private string GenerateMessageId()
        {
            return _messageIdGenerator.Generate();
        }
    }

    public class TransactionIdGenerator
    {
        public string Generate()
        {
            return Guid.NewGuid().ToString();
        }
    }

    public class MessageIdGenerator
    {
        public string Generate()
        {
            return Guid.NewGuid().ToString();
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }

    public class MessageQueue
    {
        public List<AcceptMessage> Messages { get; } = new();

        public void Add(AcceptMessage acceptMessage)
        {
            Messages.Add(acceptMessage);
        }
    }

    public class AcceptMessage
    {
        public string MessagePayload { get; init; }
    }

    public class TransactionRepository
    {
        private readonly List<AcceptedTransaction> _transactions = new();

        public void Add(AcceptedTransaction acceptedTransaction)
        {
            _transactions.Add(acceptedTransaction);
        }

        public AcceptedTransaction Get(string transactionId)
        {
            return _transactions.FirstOrDefault(transaction =>
                transaction.TransactionId.Equals(transactionId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class AcceptedTransaction
    {
        public AcceptedTransaction(string transactionId)
        {
            TransactionId = transactionId;
        }

        public string TransactionId { get; }
    }
}
