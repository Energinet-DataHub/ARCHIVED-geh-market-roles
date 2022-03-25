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
            RegisterTransaction(CreateTransaction());

            var acceptMessage = _outgoingMessages.Messages.FirstOrDefault();
            Assert.Equal("ConfirmRequestChangeOfSupplier_MarketDocument", acceptMessage.DocumentType);
            Assert.NotNull(acceptMessage);
        }

        private void RegisterTransaction(B2BTransaction transaction)
        {
            var repository = new TransactionRepository();

            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            repository.Add(acceptedTransaction);

            _outgoingMessages.Add(new AcceptMessage()
            {
                DocumentType = "ConfirmRequestChangeOfSupplier_MarketDocument",
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
        public string DocumentType { get; init; }
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
