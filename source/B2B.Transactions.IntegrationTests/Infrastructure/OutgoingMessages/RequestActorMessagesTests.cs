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
using Xunit;

namespace B2B.Transactions.IntegrationTests.Infrastructure.OutgoingMessages
{
    public class MessageRequestTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageFactory<IDocument> _messageFactory;
        private readonly MessageForwarderSpy _messageForwarder;

        public MessageRequestTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messageFactory = GetService<IMessageFactory<IDocument>>();
            _messageForwarder = new MessageForwarderSpy(_outgoingMessageStore);
        }

        [Fact]
        public async Task Message_is_forwarded_on_request()
        {
            var messageIdsToForward = new List<Guid>() { CreateOutgoingMessage().Id, CreateOutgoingMessage().Id };

            await _messageForwarder.ForwardAsync(messageIdsToForward).ConfigureAwait(false);

            Assert.NotNull(_messageForwarder.ForwardedMessages);
        }

        [Fact]
        public async Task Throw_if_message_does_not_exist()
        {
            var nonExistingMessage = new List<Guid>() { Guid.NewGuid() };

            await Assert.ThrowsAsync<OutgoingMessageNotFoundException>(() => _messageForwarder.ForwardAsync(nonExistingMessage))
                .ConfigureAwait(false);
        }

        private OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = TransactionBuilder.CreateTransaction();
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage =
                new OutgoingMessage(document.DocumentType, document.MessagePayload, transaction.Message.ReceiverId, Guid.NewGuid().ToString());
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
            foreach (var messageId in messageIdsToForward)
            {
                var message = _outgoingMessageStore.GetMessage(messageId);
                if (message is null)
                {
                    throw new OutgoingMessageNotFoundException(messageId);
                }

                ForwardedMessages.Add(messageId);
            }
            return Task.CompletedTask;
        }

        public List<Guid> ForwardedMessages { get; } = new ();
    }

    public class OutgoingMessageNotFoundException : Exception
    {
        public OutgoingMessageNotFoundException(Guid messageId)
            : base($"Message with id:{messageId} does not exist")
        {
        }
    }
}
