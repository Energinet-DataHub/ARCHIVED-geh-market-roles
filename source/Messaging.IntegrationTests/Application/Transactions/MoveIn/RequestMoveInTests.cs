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
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Xml;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.IncomingMessages.SchemaStore;
using Messaging.Infrastructure.Transactions;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn
{
    [IntegrationTest]
    public class RequestMoveInTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;

        public RequestMoveInTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
        }

        [Fact]
        public async Task Transaction_is_started()
        {
            var incomingMessage = MessageBuilder()
                .WithSenderId(SampleData.SenderId)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

            AssertTransaction.Transaction(SampleData.TransactionId, GetService<IEdiDatabaseConnection>())
                .HasState(MoveInTransaction.State.Started)
                .HasStartedByMessageId(incomingMessage.Message.MessageId)
                .HasNewEnergySupplierId(incomingMessage.Message.SenderId)
                .HasConsumerId(incomingMessage.MarketActivityRecord.ConsumerId!)
                .HasConsumerName(incomingMessage.MarketActivityRecord.ConsumerName!)
                .HasConsumerIdType(incomingMessage.MarketActivityRecord.ConsumerIdType!)
                .HasEndOfSupplyNotificationState(MoveInTransaction.NotificationState.NotNeeded)
                .HasGridOperatorNotificationState(MoveInTransaction.NotificationState.Pending)
                .HasCustomerMasterDataSentToGridOperatorState(MoveInTransaction.MasterDataState.Pending)
                .HasRequestedByActorNumber(SampleData.SenderId);
        }

        [Fact]
        public async Task A_confirm_message_created_when_the_transaction_is_accepted()
        {
            await GivenRequestHasBeenAccepted().ConfigureAwait(false);

            var confirmMessage = _outgoingMessageStore.GetByTransactionId(SampleData.TransactionId)!;

            await AsserConfirmMessage(confirmMessage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Fetch_metering_point_master_data_when_the_transaction_is_accepted()
        {
            await GivenRequestHasBeenAccepted().ConfigureAwait(false);

            AssertCommand<FetchMeteringPointMasterData>();
        }

        [Fact]
        public async Task Customer_master_data_is_retrieved_when_the_transaction_is_accepted()
        {
            await GivenRequestHasBeenAccepted().ConfigureAwait(false);

            AssertCommand<FetchCustomerMasterData>();
        }

        [Fact]
        public async Task A_reject_message_is_created_when_the_transaction_is_rejected()
        {
            var httpClientMock = GetHttpClientMock();
            httpClientMock.RespondWithValidationErrors(new List<string> { "InvalidConsumer" });

            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn.Code)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId(SampleData.SenderId)
                .WithConsumerName(null)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
            var rejectMessage = _outgoingMessageStore.GetByTransactionId(SampleData.TransactionId)!;

            await AssertRejectMessage(rejectMessage).ConfigureAwait(false);
        }

        [Fact]
        public async Task A_reject_message_is_created_when_the_sender_id_does_not_match_energy_supplier_id()
        {
            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn.Code)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId("1234567890123")
                .WithConsumerName(null)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
            var rejectMessage = _outgoingMessageStore.GetByTransactionId(SampleData.TransactionId)!;

            await AssertRejectMessage(rejectMessage).ConfigureAwait(false);
        }

        [Fact]
        public async Task A_reject_message_is_created_when_the_energy_supplier_id_is_empty()
        {
            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn.Code)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId(SampleData.SenderId)
                .WithEnergySupplierId(null)
                .WithConsumerName(null)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
            var rejectMessage = _outgoingMessageStore.GetByTransactionId(SampleData.TransactionId)!;

            await AssertRejectMessage(rejectMessage).ConfigureAwait(false);
        }

        private static void AssertHeader(XDocument document, OutgoingMessage message, string expectedReasonCode)
        {
            Assert.NotEmpty(AssertXmlMessage.GetMessageHeaderValue(document, "mRID")!);
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");
            AssertXmlMessage.AssertHasHeaderValue(document, "process.processType", message.ProcessType);
            AssertXmlMessage.AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", message.SenderId.Value);
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", message.SenderRole.ToString());
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", message.ReceiverId.Value);
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", message.ReceiverRole.ToString());
            AssertXmlMessage.AssertHasHeaderValue(document, "reason.code", expectedReasonCode);
        }

        private static async Task ValidateDocument(Stream dispatchedDocument, string schemaName, string schemaVersion)
        {
            var schemaProvider = new XmlSchemaProvider();
            var schema = await schemaProvider.GetSchemaAsync<XmlSchema>(schemaName, schemaVersion).ConfigureAwait(false);

            var validationResult = await MessageValidator.ValidateAsync(dispatchedDocument, schema!);
            Assert.True(validationResult.IsValid);
        }

        private static IncomingMessageBuilder MessageBuilder()
        {
            return new IncomingMessageBuilder()
                .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
                .WithMessageId(SampleData.OriginalMessageId)
                .WithTransactionId(SampleData.TransactionId);
        }

        private async Task GivenRequestHasBeenAccepted()
        {
            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn.Code)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId(SampleData.SenderId)
                .WithConsumerName(SampleData.ConsumerName)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        }

        private async Task AssertRejectMessage(OutgoingMessage rejectMessage)
        {
            var peekResult = await InvokeCommandAsync(new PeekRequest(
                rejectMessage.ReceiverId,
                MessageCategory.MasterData));

            await ValidateDocument(peekResult.Bundle!, "rejectrequestchangeofsupplier", "0.1").ConfigureAwait(false);

            var document = XDocument.Load(peekResult.Bundle!);
            AssertHeader(document, rejectMessage, "A02");
        }

        private async Task AsserConfirmMessage(OutgoingMessage message)
        {
            var peekResult = await InvokeCommandAsync(new PeekRequest(
                ActorNumber.Create(SampleData.NewEnergySupplierNumber),
                MessageCategory.MasterData));

            await ValidateDocument(peekResult.Bundle!, "confirmrequestchangeofsupplier", "0.1").ConfigureAwait(false);

            var document = XDocument.Load(peekResult.Bundle!);
            AssertHeader(document, message, "A01");
        }

        private HttpClientSpy GetHttpClientMock()
        {
            var adapter = GetService<IHttpClientAdapter>();
            return adapter as HttpClientSpy ?? throw new InvalidCastException();
        }

        private AssertQueuedCommand AssertCommand<TCommand>()
        {
            return AssertQueuedCommand.QueuedCommand<TCommand>(
                GetService<IEdiDatabaseConnection>(),
                GetService<InternalCommandMapper>());
        }
    }
}
