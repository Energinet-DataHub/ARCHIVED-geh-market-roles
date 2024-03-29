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
using Messaging.Application.Configuration;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using NodaTime;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.IntegrationTests.Application.IncomingMessages
{
    internal class IncomingMessageBuilder
    {
        private const string NotSet = "NotSet";
        private readonly string _createdAt = SystemClock.Instance.GetCurrentInstant().ToString();
        private readonly string _receiverRole = "DDQ";
        private readonly string _senderRole = "DDZ";
        private Instant _effectiveDate = SystemClock.Instance.GetCurrentInstant();
        private string? _messageId;
        private string _processType = NotSet;
        private string _senderId = NotSet;
        private ActorNumber _receiverId = DataHubDetails.IdentificationNumber;
        private string? _consumerName = NotSet;
        private string _marketEvaluationPointId = NotSet;
        private string? _transactionId;
        private string? _energySupplierId = "123456";
        private string _consumerId = "NotSet";

        internal IncomingMessageBuilder WithConsumerId(string id)
        {
            _consumerId = id;
            return this;
        }

        internal IncomingMessageBuilder WithEffectiveDate(Instant effectiveDate)
        {
            _effectiveDate = effectiveDate;
            return this;
        }

        internal IncomingMessageBuilder WithMarketEvaluationPointId(string marketEvaluationPointId)
        {
            _marketEvaluationPointId = marketEvaluationPointId;
            return this;
        }

        internal IncomingMessageBuilder WithProcessType(string processType)
        {
            _processType = processType;
            return this;
        }

        internal IncomingMessageBuilder WithSenderId(string senderId)
        {
            _senderId = senderId;
            return this;
        }

        internal IncomingMessageBuilder WithConsumerName(string? consumerName)
        {
            _consumerName = consumerName;
            return this;
        }

        internal IncomingMessageBuilder WithReceiver(string receiverId)
        {
            _receiverId = ActorNumber.Create(receiverId);
            return this;
        }

        internal IncomingMessageBuilder WithTransactionId(string transactionId)
        {
            _transactionId = transactionId;
            return this;
        }

        internal IncomingMessageBuilder WithMessageId(string originalMessageId)
        {
            _messageId = originalMessageId;
            return this;
        }

        internal IncomingMessageBuilder WithEnergySupplierId(string? energySupplierId)
        {
            _energySupplierId = energySupplierId;
            return this;
        }

        internal RequestChangeOfSupplierTransaction Build()
        {
            return RequestChangeOfSupplierTransaction.Create(
                CreateHeader(),
                CreateMarketActivityRecord());
        }

        private MarketActivityRecord CreateMarketActivityRecord()
        {
            return new MarketActivityRecord()
            {
                BalanceResponsibleId = "fake",
                Id = _transactionId ?? Guid.NewGuid().ToString(),
                ConsumerId = _consumerId,
                ConsumerName = _consumerName,
                EffectiveDate = _effectiveDate.ToString(),
                EnergySupplierId = _energySupplierId,
                MarketEvaluationPointId = _marketEvaluationPointId,
            };
        }

        private MessageHeader CreateHeader()
        {
            return new MessageHeader(
                _messageId ?? Guid.NewGuid().ToString(),
                _processType,
                _senderId,
                _senderRole,
                _receiverId.Value,
                _receiverRole,
                _createdAt);
        }
    }
}
