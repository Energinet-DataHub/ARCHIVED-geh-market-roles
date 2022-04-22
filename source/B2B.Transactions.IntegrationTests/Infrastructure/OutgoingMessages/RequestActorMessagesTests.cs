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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using B2B.Transactions.DataAccess;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
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
            var timeProvider = GetService<ISystemDateTimeProvider>();
            var messageValidator = GetService<MessageValidator>();
            _messageForwarder = new MessageForwarderSpy(_outgoingMessageStore, timeProvider, messageValidator);
        }

        [Fact]
        public async Task Message_is_forwarded_on_request()
        {
            var messageIdsToForward = new List<Guid> { CreateOutgoingMessageOld().Id, CreateOutgoingMessageOld().Id };

            await _messageForwarder.ForwardAsync(messageIdsToForward).ConfigureAwait(false);

            Assert.NotNull(_messageForwarder.ForwardedMessages);
        }

        [Fact]
        public async Task Result_contains_exception_if_message_does_not_exist()
        {
            var nonExistingMessage = new List<Guid> { Guid.NewGuid() };

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
            var marketActivityRecords = AssertXmlMessage.GetMarketActivityRecords(bundledMessage);
            Assert.Equal(2, marketActivityRecords.Count);

            AssertMarketActivityRecord(bundledMessage, message1);
            AssertMarketActivityRecord(bundledMessage, message2);
            AssertMessageHeader(bundledMessage);
        }

        private static void AssertMarketActivityRecord(XDocument document, OutgoingMessage message)
        {
            var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, message.Id.ToString())!;
            Assert.NotNull(marketActivityRecord);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "originalTransactionIDReference_MktActivityRecord.mRID", message.OriginalTransactionId);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "marketEvaluationPoint.mRID", message.MarketEvaluationPointId);
        }

        private static void AssertMessageHeader(XDocument document)
        {
            Assert.NotEmpty(AssertXmlMessage.GetMessageHeaderValue(document, "mRID")!);
        }

        private static OutgoingMessage CreateOutgoingMessage(IDocument document, IncomingMessage transaction)
        {
            return new OutgoingMessage(document.DocumentType, transaction.Message.ReceiverId, Guid.NewGuid().ToString(), transaction.MarketActivityRecord.Id, transaction.MarketActivityRecord.MarketEvaluationPointId);
        }

        private OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = IncomingMessageBuilder.CreateMessage();
            var document = _messageFactory.CreateMessage(transaction);
            return CreateOutgoingMessage(document, transaction);
        }

        private OutgoingMessage CreateOutgoingMessageOld() // TODO: Refactor
        {
            var transaction = IncomingMessageBuilder.CreateMessage();
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage = CreateOutgoingMessage(document, transaction);
            _outgoingMessageStore.Add(outgoingMessage);
            return outgoingMessage;
        }
    }

#pragma warning disable
    public class MessageForwarderSpy
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly MessageValidator _messageValidator;
        public List<Guid> ForwardedMessages { get; } = new ();

        public MessageForwarderSpy(
            IOutgoingMessageStore outgoingMessageStore,
            ISystemDateTimeProvider systemDateTimeProvider,
            MessageValidator messageValidator)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _systemDateTimeProvider = systemDateTimeProvider;
            _messageValidator = messageValidator;
        }

        public Task<Result> ForwardAsync(List<Guid> messageIdsToForward)
        {
            var exceptions = new List<Exception>();
            var messages = new List<OutgoingMessage>();

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
                    messages.Add(message);
                }
            }

            return Task.FromResult(exceptions.Count == 0
                ? new Result(CreateBundledMessage(messages))
                : new Result(exceptions)) ;
        }


        private Stream CreateBundledMessage(List<OutgoingMessage> messages)
        {
            const string MessageType = "ConfirmRequestChangeOfSupplier";
            const string Prefix = "cim";

            var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8 };
            using var stream = new MemoryStream();
            using var output = new Utf8StringWriter();
            using var writer = XmlWriter.Create(output, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement(Prefix, "ConfirmRequestChangeOfSupplier_MarketDocument", "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd");
            writer.WriteElementString(Prefix, "mRID", null, GenerateMessageId());
            writer.WriteElementString(Prefix, "type", null, "414");
            // writer.WriteElementString(Prefix, "process.processType", null, transaction?.Message.ProcessType);
            // writer.WriteElementString(Prefix, "businessSector.type", null, "23");
            //
            // writer.WriteStartElement(Prefix, "sender_MarketParticipant.mRID", null);
            // writer.WriteAttributeString(null, "codingScheme", null, "A10");
            // writer.WriteValue("5790001330552");
            // writer.WriteEndElement();
            //
            // writer.WriteElementString(Prefix, "sender_MarketParticipant.marketRole.type", null, "DDZ");
            //
            // writer.WriteStartElement(Prefix, "receiver_MarketParticipant.mRID", null);
            // writer.WriteAttributeString(null, "codingScheme", null, "A10");
            // writer.WriteValue(transaction?.Message.SenderId);
            // writer.WriteEndElement();
            //
            // writer.WriteElementString(Prefix, "receiver_MarketParticipant.marketRole.type", null, transaction?.Message.SenderRole);
            // writer.WriteElementString(Prefix, "createdDateTime", null, GetCurrentDateTime());
            // writer.WriteElementString(Prefix, "reason.code", null, "A01");
            //

            foreach (var message in messages)
            {
                writer.WriteStartElement(Prefix, "MktActivityRecord", null);
                writer.WriteElementString(Prefix, "mRID", null, message.Id.ToString());
                writer.WriteElementString(Prefix, "originalTransactionIDReference_MktActivityRecord.mRID", null, message.OriginalTransactionId);
                writer.WriteStartElement(Prefix, "marketEvaluationPoint.mRID", null);
                writer.WriteAttributeString(null, "codingScheme", null, "A10");
                writer.WriteValue(message.MarketEvaluationPointId);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            // writer.WriteEndElement();
            // writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Close();
            output.Flush();

            // var parseResult = _messageValidator.ParseAsync(output.ToString(), "confirmrequestchangeofsupplier", "1.0");
            // if (!_messageValidator.Success)
            // {
            //     throw new InvalidOperationException($"Generated accept message does not conform with XSD schema definition: {_messageValidator.Errors()}");
            // }

            var data = Encoding.UTF8.GetBytes(output.ToString());

            return new MemoryStream(data);
        }

        protected string GenerateMessageId()
        {
            return MessageIdGenerator.Generate();
        }

        protected string GenerateTransactionId()
        {
            return TransactionIdGenerator.Generate();
        }

        protected string GetCurrentDateTime()
        {
            return _systemDateTimeProvider.Now().ToString();
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
