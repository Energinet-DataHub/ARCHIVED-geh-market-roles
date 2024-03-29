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
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.Actors;
using Messaging.Infrastructure.Configuration.Serialization;
using Xunit;

namespace Messaging.IntegrationTests.Assertions
{
    public class AssertOutgoingMessage
    {
        private readonly ISerializer _serializer = new Serializer();
        private readonly dynamic _message;

        private AssertOutgoingMessage(dynamic message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Assert.NotNull(message);
            _message = message;
        }

        public static async Task<AssertOutgoingMessage> OutgoingMessageAsync(Guid transactionId, string messageType, string processType, IDatabaseConnectionFactory connectionFactoryFactory)
        {
            if (connectionFactoryFactory == null) throw new ArgumentNullException(nameof(connectionFactoryFactory));
            using var connection = await connectionFactoryFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
            var message = connection.QuerySingle(
                $"SELECT m.Id, m.RecordId, m.MessageType, m.ReceiverId, m.TransactionId, m.ProcessType," +
                $"m.ReceiverRole, m.SenderId, m.SenderRole, m.MessageRecord " +
                $" FROM [b2b].[OutgoingMessages] m" +
                $" WHERE m.TransactionId = '{transactionId}' AND m.MessageType = '{messageType}' AND m.ProcessType = '{processType}'");

            Assert.NotNull(message);
            return new AssertOutgoingMessage(message);
        }

        public static async Task<AssertOutgoingMessage> OutgoingMessageAsync(Guid transactionId, string messageType, string processType, MarketRole receiverRole, IDatabaseConnectionFactory connectionFactoryFactory)
        {
            if (connectionFactoryFactory == null) throw new ArgumentNullException(nameof(connectionFactoryFactory));
            ArgumentNullException.ThrowIfNull(receiverRole);
            using var connection = await connectionFactoryFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
            var message = connection.QuerySingle(
                $"SELECT m.Id, m.RecordId, m.MessageType, m.ReceiverId, m.TransactionId, m.ProcessType," +
                $"m.ReceiverRole, m.SenderId, m.SenderRole, m.MessageRecord " +
                $" FROM [b2b].[OutgoingMessages] m" +
                $" WHERE m.TransactionId = '{transactionId}' AND m.MessageType = '{messageType}' AND m.ProcessType = '{processType}' AND m.ReceiverRole = '{receiverRole.Name}'");

            Assert.NotNull(message);
            return new AssertOutgoingMessage(message);
        }

        public static async Task<AssertOutgoingMessage> OutgoingMessageAsync(string messageType, string processType, MarketRole receiverRole, IDatabaseConnectionFactory connectionFactoryFactory)
        {
            if (connectionFactoryFactory == null) throw new ArgumentNullException(nameof(connectionFactoryFactory));
            ArgumentNullException.ThrowIfNull(receiverRole);
            using var connection = await connectionFactoryFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
            var message = connection.QuerySingle(
                $"SELECT m.Id, m.RecordId, m.MessageType, m.ReceiverId, m.TransactionId, m.ProcessType," +
                $"m.ReceiverRole, m.SenderId, m.SenderRole, m.MessageRecord " +
                $" FROM [b2b].[OutgoingMessages] m" +
                $" WHERE m.MessageType = '{messageType}' AND m.ProcessType = '{processType}' AND m.ReceiverRole = '{receiverRole.Name}'");

            Assert.NotNull(message);
            return new AssertOutgoingMessage(message);
        }

        public AssertOutgoingMessage HasReceiverId(string receiverId)
        {
            Assert.Equal(receiverId, _message.ReceiverId);
            return this;
        }

        public AssertOutgoingMessage HasReceiverRole(string receiverRole)
        {
            Assert.Equal(receiverRole, _message.ReceiverRole);
            return this;
        }

        public AssertOutgoingMessage HasSenderId(string senderId)
        {
            Assert.Equal(senderId, _message.SenderId);
            return this;
        }

        public AssertOutgoingMessage HasSenderRole(string senderRole)
        {
            Assert.Equal(senderRole, _message.SenderRole);
            return this;
        }

        public AssertMarketActivityRecord WithMarketActivityRecord()
        {
            return new AssertMarketActivityRecord(_message.MessageRecord);
        }

        public AssertOutgoingMessage HasMessageRecordValue<TMessageRecord>(Func<TMessageRecord, object> propertySelector, object expectedValue)
        {
            var sut = _serializer.Deserialize<TMessageRecord>(_message.MessageRecord);
            Assert.Equal(expectedValue, propertySelector(sut));
            return this;
        }

        public AssertOutgoingMessage HasMessageRecordValue<TMessageRecord, TValueType>(Func<TMessageRecord, TValueType> propertySelector, TValueType expectedValue)
        {
            var sut = _serializer.Deserialize<TMessageRecord>(_message.MessageRecord);
            Assert.Equal(expectedValue, propertySelector(sut));
            return this;
        }
    }
}
