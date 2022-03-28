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
using System.Xml.Linq;
using B2B.Transactions.Messages;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using Xunit;

namespace B2B.Transactions.Tests
{
#pragma warning disable
    public class TransactionHandlingTests
    {
        private readonly TransactionRepository _transactionRepository = new();
        private readonly SystemDateTimeProviderStub _dateTimeProvider = new();
        private readonly XNamespace _namespace = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
        private OutgoingMessages.OutgoingMessageStore _outgoingMessageStore = new();

        [Fact]
        public async Task Transaction_is_registered()
        {
            var transaction = CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.Get(transaction.Message.MessageId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Accept_message_is_sent_to_sender_when_transaction_is_accepted()
        {
            var now = _dateTimeProvider.Now();
            _dateTimeProvider.SetNow(now);
            var transaction = CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var acceptMessage = _outgoingMessageStore.Messages.FirstOrDefault();
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
            var useCase = new RegisterTransaction(_outgoingMessageStore,
                _dateTimeProvider, _transactionRepository);
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
}
