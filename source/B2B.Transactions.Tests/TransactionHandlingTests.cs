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
            Assert.NotNull(GetValue(acceptMessage, "mRID"));
            AssertHasValue(acceptMessage, "type", "414");

            Assert.NotNull(acceptMessage);
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
            writer.WriteEndElement();
            writer.Close();



                // <cim:type>414</cim:type>
                // <cim:process.processType>E03</cim:process.processType>
                // <cim:businessSector.type>23</cim:businessSector.type>
                // <cim:sender_MarketParticipant.mRID codingScheme="A10">5799999933318</cim:sender_MarketParticipant.mRID>
                // <cim:sender_MarketParticipant.marketRole.type>DDZ</cim:sender_MarketParticipant.marketRole.type>
                // <cim:receiver_MarketParticipant.mRID codingScheme="A10">5790001330552</cim:receiver_MarketParticipant.mRID>
                // <cim:receiver_MarketParticipant.marketRole.type>DDQ</cim:receiver_MarketParticipant.marketRole.type>
                // <cim:createdDateTime>2022-09-07T09:30:47Z</cim:createdDateTime>
                // <cim:reason.code>A01</cim:reason.code>


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

        private static void AssertHasValue(AcceptMessage message, string elementName, string expectedValue)
        {
            Assert.Equal(expectedValue, GetValue(message, elementName));
        }

        private static string? GetValue(AcceptMessage message, string elementName)
        {
            var document = XDocument.Parse(message.MessagePayload);
            XNamespace ns = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";

            return document?.Element(ns + "ConfirmRequestChangeOfSupplier_MarketDocument").Element(elementName).Value;
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
