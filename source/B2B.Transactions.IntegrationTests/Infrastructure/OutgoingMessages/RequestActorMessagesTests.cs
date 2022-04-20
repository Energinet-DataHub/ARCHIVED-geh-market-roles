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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using B2B.Transactions.DataAccess;
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
            var messageIdsToForward = new List<Guid>() { CreateOutgoingMessageOld().Id, CreateOutgoingMessageOld().Id };

            await _messageForwarder.ForwardAsync(messageIdsToForward).ConfigureAwait(false);

            Assert.NotNull(_messageForwarder.ForwardedMessages);
        }

        [Fact]
        public async Task Result_contains_exception_if_message_does_not_exist()
        {
            var nonExistingMessage = new List<Guid>() { Guid.NewGuid() };

            var result = await _messageForwarder.ForwardAsync(nonExistingMessage).ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is OutgoingMessageNotFoundException);
        }

        [Fact]
        public async Task Requested_messages_are_bundled_in_a_bundle_message()
        {
            var message1 = CreateOutgoingMessage();
            var message2 = CreateOutgoingMessage();
            _outgoingMessageStore.Add(message1);
            _outgoingMessageStore.Add(message2);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);

            var result = await _messageForwarder.ForwardAsync(new List<Guid> { message1.Id, message2.Id }).ConfigureAwait(false);

            Assert.NotNull(result.BundledMessage);

            var bundledMessage = XDocument.Load(result.BundledMessage);
        }

        private OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = TransactionBuilder.CreateTransaction();
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage =
                new OutgoingMessage(document.DocumentType, document.MessagePayload, transaction.Message.ReceiverId, Guid.NewGuid().ToString());

            return outgoingMessage;
        }

        private OutgoingMessage CreateOutgoingMessageOld()
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
        public List<Guid> ForwardedMessages { get; } = new ();

        public MessageForwarderSpy(IOutgoingMessageStore outgoingMessageStore)
        {
            _outgoingMessageStore = outgoingMessageStore;
        }

        public Task<Result> ForwardAsync(List<Guid> messageIdsToForward)
        {
            var exceptions = new List<Exception>();

            foreach (var messageId in messageIdsToForward)
            {
                var message = _outgoingMessageStore.GetById(messageId);
                if (message is null)
                {
                    exceptions.Add(new OutgoingMessageNotFoundException(messageId));
                }
                else
                {
                    ForwardedMessages.Add(messageId);
                }
            }

            return Task.FromResult(exceptions.Count == 0 ? new Result(CreateBundledMessage()) : new Result(exceptions)) ;
        }

        private Stream CreateBundledMessage()
        {
            var stream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(stream);
            xmlWriter.WriteStartDocument();
            xmlWriter.Close();

            return stream;
        }
    }

    public class Result
    {
        public Result(List<Exception> exceptions)
        {
            Errors = exceptions;
        }

        public Result(Stream bundledMessage)
        {
            BundledMessage = bundledMessage;
        }

        public IReadOnlyCollection<Exception> Errors { get; }
        public Stream BundledMessage { get; }
    }

    public class OutgoingMessageNotFoundException : Exception
    {
        public OutgoingMessageNotFoundException(Guid messageId)
            : base($"Message with id:{messageId} does not exist")
        {
        }
    }
}
