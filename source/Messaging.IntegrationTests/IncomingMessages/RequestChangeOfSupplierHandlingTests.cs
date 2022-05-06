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

using System.Threading.Tasks;
using Messaging.Application.IncomingMessages;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions;
using Messaging.Application.Xml;
using Messaging.Application.Xml.SchemaStore;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace Messaging.IntegrationTests.IncomingMessages
{
    [IntegrationTest]
    public class RequestChangeOfSupplierHandlingTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly ITransactionRepository _transactionRepository;
        private readonly RequestChangeOfSupplierHandler _requestChangeOfSupplierHandler;

        public RequestChangeOfSupplierHandlingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _transactionRepository =
                GetService<ITransactionRepository>();
            _requestChangeOfSupplierHandler = GetService<RequestChangeOfSupplierHandler>();
        }

        [Fact]
        public async Task Transaction_is_registered()
        {
            var incomingMessage = IncomingMessageBuilder.CreateMessage();

            await _requestChangeOfSupplierHandler.HandleAsync(incomingMessage).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.GetById(incomingMessage.MarketActivityRecord.Id);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Reject_if_business_request_is_invalid()
        {
            var messageBuilder = new IncomingMessageBuilder();
            var incomingMessage = messageBuilder
                .WithProcessType("E03")
                .WithConsumerName(null)
                .Build();

            await _requestChangeOfSupplierHandler.HandleAsync(incomingMessage).ConfigureAwait(false);

            var rejectMessage = _outgoingMessageStore.GetByOriginalMessageId(incomingMessage.Id)!;
            Assert.NotNull(rejectMessage);
            await AssertOutgoingMessage(rejectMessage);

            var messageDispatcher = GetService<IMessageDispatcher>() as MessageDispatcherSpy;
            var schema = await GetService<ISchemaProvider>().GetSchemaAsync("rejectrequestchangeofsupplier", "1.0").ConfigureAwait(false);
            var valid = await MessageValidator.ValidateAsync(messageDispatcher!.DispatchedMessage!, schema!);
            Assert.True(valid.IsValid);
        }

        private async Task AssertOutgoingMessage(OutgoingMessage message)
        {
            var result = await GetService<MessageRequestHandler>().HandleAsync(new[] { message.Id.ToString(), });
            Assert.True(result.Success);
        }
    }
}
