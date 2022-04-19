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
using System.Threading.Tasks;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Xunit;

namespace B2B.Transactions.IntegrationTests.Infrastructure.OutgoingMessages
{
    public class MessageRequestTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageFactory<IDocument> _messageFactory;

        public MessageRequestTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            var systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messageFactory = new AcceptMessageFactory(systemDateTimeProvider);
        }

        [Fact]
        public async Task Message_is_forwarded_on_request()
        {
            var messageIdsToForward = new List<Guid>() { CreateOutgoingMessage().Id, CreateOutgoingMessage().Id };
            var messageForwarder = new MessageForwarderSpy();

            await messageForwarder.ForwardAsync(messageIdsToForward).ConfigureAwait(false);

            Assert.NotNull(messageForwarder.ForwardedMessages);
        }

        private OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = TransactionBuilder.CreateTransaction();
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage =
                new OutgoingMessage(document.DocumentType, document.MessagePayload, transaction.Message.ReceiverId);
            _outgoingMessageStore.Add(outgoingMessage);
            return outgoingMessage;
        }
    }

#pragma warning disable
    public class MessageForwarderSpy
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;

        public MessageForwarderSpy(IOutgoingMessageStore outgoingMessageStore)
        {
            _outgoingMessageStore = outgoingMessageStore;
        }
        public Task ForwardAsync(List<Guid> messageIdsToForward)
        {
            var messagesToForward = _outgoingMessageStore.GetMessagesToForward(messageIdsToForward);


            foreach (var messageId in messageIdsToForward)
            {
                ForwardedMessages.Add(messageId);
            }
            return Task.CompletedTask;
        }

        public List<Guid> ForwardedMessages { get; } = new ();
    }
}
