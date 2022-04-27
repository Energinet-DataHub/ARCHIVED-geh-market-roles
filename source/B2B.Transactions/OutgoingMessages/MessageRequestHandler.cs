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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B2B.Transactions.IncomingMessages;
using MarketActivityRecord = B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord;

namespace B2B.Transactions.OutgoingMessages
{
    public class MessageRequestHandler
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IncomingMessageStore _incomingMessageStore;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly MessageFactory _messageFactory;

        public MessageRequestHandler(
            IOutgoingMessageStore outgoingMessageStore,
            MessageDispatcher messageDispatcher,
            MessageFactory messageFactory,
            IncomingMessageStore incomingMessageStore)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageDispatcher = messageDispatcher;
            _messageFactory = messageFactory;
            _incomingMessageStore = incomingMessageStore;
        }

        public async Task<Result> HandleAsync(IReadOnlyCollection<string> messageIdsToForward)
        {
            var exceptions = new List<Exception>();
            var messages = _outgoingMessageStore.GetByIds(messageIdsToForward);

            exceptions.AddRange(messageIdsToForward
                .Except(messages.Select(message => message.Id.ToString()))
                .Select(messageId => new OutgoingMessageNotFoundException(messageId))
                .ToArray());

            if (exceptions.Any())
            {
                return Result.Failure(exceptions.ToArray());
            }

            var expectedProcessType = messages.First().ProcessType;
            if (messages.Any(message =>
                    message.ProcessType.Equals(expectedProcessType, StringComparison.OrdinalIgnoreCase) == false))
            {
                return Result.Failure(new ProcessTypesDoesNotMatchException(messageIdsToForward.ToArray()));
            }

            var message = await CreateMessageFromAsync(messages).ConfigureAwait(false);
            await _messageDispatcher.DispatchAsync(message).ConfigureAwait(false);

            return Result.Succeeded();
        }

        private Task<Stream> CreateMessageFromAsync(ReadOnlyCollection<OutgoingMessage> outgoingMessages)
        {
            var incomingMessage = _incomingMessageStore.GetById(outgoingMessages[0].OriginalMessageId);
            var messageHeader = new MessageHeader(incomingMessage!.Message.ProcessType, incomingMessage.Message.ReceiverId, incomingMessage.Message.ReceiverRole, incomingMessage.Message.SenderId, incomingMessage.Message.SenderRole);
            var marketActivityRecords = new List<MarketActivityRecord>();
            foreach (var outgoingMessage in outgoingMessages)
            {
                marketActivityRecords.Add(
                    new MarketActivityRecord(outgoingMessage.Id.ToString(), incomingMessage.MarketActivityRecord.Id, incomingMessage.MarketActivityRecord.MarketEvaluationPointId));
            }

            return _messageFactory.CreateFromAsync(messageHeader, marketActivityRecords.AsReadOnly());
        }
    }
}
