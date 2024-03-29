﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Threading.Tasks;
using Messaging.Application.IncomingMessages;
using Messaging.CimMessageAdapter.Errors;
using Messaging.CimMessageAdapter.Messages.Queues;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.CimMessageAdapter.Messages
{
    public abstract class MessageReceiver<TQueue>
        where TQueue : Queue
    {
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIds _messageIds;
        private readonly IMessageQueueDispatcher<TQueue> _messageQueueDispatcher;
        private readonly ITransactionIds _transactionIds;
        private readonly ISenderAuthorizer _senderAuthorizer;

        protected MessageReceiver(IMessageIds messageIds, IMessageQueueDispatcher<TQueue> messageQueueDispatcher, ITransactionIds transactionIds, ISenderAuthorizer senderAuthorizer)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _messageQueueDispatcher = messageQueueDispatcher ??
                                             throw new ArgumentNullException(nameof(messageQueueDispatcher));
            _transactionIds = transactionIds;
            _senderAuthorizer = senderAuthorizer;
        }

        public async Task<Result> ReceiveAsync<TMarketActivityRecordType, TMarketTransactionType>(MessageParserResult<TMarketActivityRecordType, TMarketTransactionType> messageParserResult)
            where TMarketActivityRecordType : IMarketActivityRecord
            where TMarketTransactionType : IMarketTransaction<TMarketActivityRecordType>
        {
            ArgumentNullException.ThrowIfNull(messageParserResult);

            var messageHeader = messageParserResult.IncomingMarketDocument?.Header;
            var marketDocument = messageParserResult.IncomingMarketDocument;

            if (InvalidMessageHeader(messageHeader))
            {
                return Result.Failure(messageParserResult.Errors.ToArray());
            }

            ArgumentNullException.ThrowIfNull(messageHeader);
            ArgumentNullException.ThrowIfNull(marketDocument);

            await AuthorizeSenderAsync(messageHeader).ConfigureAwait(false);
            await VerifyReceiverAsync(messageHeader).ConfigureAwait(false);
            if (MessageIdIsEmpty(messageHeader.MessageId))
            {
                return Result.Failure(_errors.ToArray());
            }

            await CheckMessageIdAsync(messageHeader.MessageId).ConfigureAwait(false);
            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            foreach (var transaction in marketDocument.ToTransactions())
            {
                if (string.IsNullOrEmpty(transaction.MarketActivityRecord.Id))
                {
                    return Result.Failure(new EmptyTransactionId(transaction.MarketActivityRecord.Id));
                }

                if (await CheckTransactionIdAsync(transaction.MarketActivityRecord.Id).ConfigureAwait(false) == false)
                {
                    return Result.Failure(new DuplicateTransactionIdDetected(transaction.MarketActivityRecord.Id));
                }

                await AddToTransactionQueueAsync(transaction).ConfigureAwait(false);
            }

            await _messageQueueDispatcher.CommitAsync().ConfigureAwait(false);
            return Result.Succeeded();
        }

        private static bool InvalidMessageHeader(MessageHeader? header)
        {
            return header is null;
        }

        private Task<bool> CheckTransactionIdAsync(string transactionId)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));
            return _transactionIds.TryStoreAsync(transactionId);
        }

        private Task AddToTransactionQueueAsync(IMarketTransaction transaction)
        {
            return _messageQueueDispatcher.AddAsync(transaction);
        }

        private bool MessageIdIsEmpty(string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            if (string.IsNullOrEmpty(messageId))
            {
                _errors.Add(new EmptyMessageId());
                return true;
            }

            return false;
        }

        private async Task CheckMessageIdAsync(string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            if (await _messageIds.TryStoreAsync(messageId).ConfigureAwait(false) == false)
            {
                _errors.Add(new DuplicateMessageIdDetected(messageId));
            }
        }

        private async Task AuthorizeSenderAsync(MessageHeader messageHeader)
        {
            if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
            var result = await _senderAuthorizer.AuthorizeAsync(messageHeader.SenderId, messageHeader.SenderRole).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task VerifyReceiverAsync(MessageHeader messageHeader)
        {
            if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
            var receiverVerification = await ReceiverVerification.VerifyAsync(messageHeader.ReceiverId, messageHeader.ReceiverRole).ConfigureAwait(false);
            _errors.AddRange(receiverVerification.Errors);
        }
    }
}
