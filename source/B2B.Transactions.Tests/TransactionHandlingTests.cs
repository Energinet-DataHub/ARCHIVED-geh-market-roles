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
using System.Text;
using System.Xml;
using System.Xml.Linq;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Transactions;
using Xunit;

namespace B2B.Transactions.Tests
{
#pragma warning disable
    public class TransactionHandlingTests
    {
        private readonly XNamespace _namespace = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
        private MessageQueue _outgoingMessages = new();


        [Fact]
        public void Transaction_is_registered()
        {
            var repository = new TransactionRepository();
            var transaction = CreateTransaction();

            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            repository.Add(acceptedTransaction);

            var savedTransaction = repository.Get(acceptedTransaction.TransactionId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public void Accept_message_is_sent_to_sender_when_transaction_is_accepted()
        {
            var transaction = CreateTransaction();
            RegisterTransaction(transaction);

            var acceptMessage = _outgoingMessages.Messages.FirstOrDefault();
            Assert.NotNull(acceptMessage);
            Assert.NotNull(GetMessageHeaderValue(acceptMessage, "mRID"));
            AssertHasHeaderValue(acceptMessage, "type", "414");

            AssertHasHeaderValue(acceptMessage, "process.processType", transaction.Message.ProcessType);
            AssertHasHeaderValue(acceptMessage, "businessSector.type", "23");
            AssertHasHeaderValue(acceptMessage, "sender_MarketParticipant.mRID", transaction.Message.SenderId);
            AssertHasHeaderValue(acceptMessage, "sender_MarketParticipant.marketRole.type", transaction.Message.SenderRole);
            AssertHasHeaderValue(acceptMessage, "receiver_MarketParticipant.mRID", transaction.Message.ReceiverId);
            AssertHasHeaderValue(acceptMessage, "receiver_MarketParticipant.marketRole.type", transaction.Message.ReceiverRole);
            AssertHasHeaderValue(acceptMessage, "createdDateTime", "2022-09-07T09:30:47Z");
            AssertHasHeaderValue(acceptMessage, "reason.code", "A01");

            Assert.NotNull(GetMarketActivityRecordValue(acceptMessage, "mRID"));
            AssertMarketActivityRecordValue(acceptMessage, "originalTransactionIDReference_MktActivityRecord.mRID", transaction.MarketActivityRecord.Id);
            AssertMarketActivityRecordValue(acceptMessage, "marketEvaluationPoint.mRID", transaction.MarketActivityRecord.MarketEvaluationPointId);
        }

        private void RegisterTransaction(B2BTransaction transaction)
        {
            var repository = new TransactionRepository();

            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            repository.Add(acceptedTransaction);

            var messageBody = new StringBuilder();
            var settings = new XmlWriterSettings() { OmitXmlDeclaration = true };

            using var writer = XmlWriter.Create(messageBody, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("cim", "ConfirmRequestChangeOfSupplier_MarketDocument", "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd");
            writer.WriteElementString("mRID", null, Guid.NewGuid().ToString());
            writer.WriteElementString("type", null, "414");
            writer.WriteElementString("process.processType", null, transaction.Message.ProcessType);
            writer.WriteElementString("businessSector.type", null, "23");

            writer.WriteStartElement("sender_MarketParticipant.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.Message.SenderId);
            writer.WriteEndElement();

            writer.WriteElementString("sender_MarketParticipant.marketRole.type", null, transaction.Message.SenderRole);

            writer.WriteStartElement("receiver_MarketParticipant.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.Message.ReceiverId);
            writer.WriteEndElement();
            writer.WriteElementString("receiver_MarketParticipant.marketRole.type", null, transaction.Message.ReceiverRole);
            writer.WriteElementString("createdDateTime", null, "2022-09-07T09:30:47Z");
            writer.WriteElementString("reason.code", null, "A01");

            writer.WriteStartElement("cim", "MktActivityRecord", null);
            writer.WriteElementString("mRID", null, Guid.NewGuid().ToString());
            writer.WriteElementString("originalTransactionIDReference_MktActivityRecord.mRID", null, transaction.MarketActivityRecord.Id);

            writer.WriteStartElement("marketEvaluationPoint.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.MarketActivityRecord.EnergySupplierId);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Close();

            _outgoingMessages.Add(new AcceptMessage()
            {
                MessagePayload = messageBody.ToString(),
            });
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
