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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B2B.CimMessageAdapter;
using B2B.CimMessageAdapter.Schema;
using MarketRoles.B2B.CimMessageAdapter.IntegrationTests.Stubs;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
    public class MessageReceiverTests : TestHost
    {
        private readonly TransactionIdsStub _transactionIdsStub = new();
        private readonly MarketActivityRecordForwarderStub _marketActivityRecordForwarderSpy = new();

        [Fact]
        public async Task Message_must_be_valid_xml()
        {
            await using var message = CreateMessageWithInvalidXmlStructure();
            {
                var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

                Assert.False(result.Success);
                Assert.Single(result.Errors);
            }
        }

        [Fact]
        public async Task Message_must_conform_to_xml_schema()
        {
            await using var message = CreateMessageNotConformingToXmlSchema();
            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_does_not_exist()
        {
            await using var message = CreateMessage();

            var result = await ReceiveRequestChangeOfSupplierMessage(message, "non_existing_version")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Return_failure_if_message_id_is_not_unique()
        {
            await using (var message = CreateMessage())
            {
                await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);
            }

            await using (var message = CreateMessage())
            {
                var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

                Assert.False(result.Success);
                Assert.Contains(result.Errors, error => error is DuplicateMessageIdDetected);
            }
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            await using var message = CreateMessageNotConformingToXmlSchema();
            await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            var activityRecord = _marketActivityRecordForwarderSpy.CommittedItems.FirstOrDefault();
            Assert.NotNull(activityRecord);
            Assert.Equal("12345699", activityRecord?.MrId);
            Assert.Equal("579999993331812345", activityRecord?.MarketEvaluationPointmRID);
            Assert.Equal("5799999933318", activityRecord?.EnergySupplierMarketParticipantmRID);
            Assert.Equal("5799999933340", activityRecord?.BalanceResponsiblePartyMarketParticipantmRID);
            Assert.Equal("0801741527", activityRecord?.CustomerMarketParticipantmRID);
            Assert.Equal("Jan Hansen", activityRecord?.CustomerMarketParticipantName);
            Assert.Equal("2022-09-07T22:00:00Z", activityRecord?.StartDateAndOrTimeDateTime);
        }

        [Fact]
        public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
        {
            await using (var message = CreateMessage())
            {
                await ReceiveRequestChangeOfSupplierMessage(message)
                    .ConfigureAwait(false);
            }

            await using (var message = CreateMessage())
            {
                await ReceiveRequestChangeOfSupplierMessage(message)
                    .ConfigureAwait(false);
            }

            Assert.Empty(_marketActivityRecordForwarderSpy.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_must_have_unique_transaction_ids()
        {
            await using var message = CreateMessageWithDuplicateTransactionIds();
            await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            Assert.Single(_marketActivityRecordForwarderSpy.CommittedItems);
        }

        private static Stream CreateMessageWithInvalidXmlStructure()
        {
            var messageStream = new MemoryStream();
            using var writer = new StreamWriter(messageStream);
            writer.Write("This is not XML");
            writer.Flush();
            messageStream.Position = 0;
            return messageStream;
        }

        private static Stream CreateMessageNotConformingToXmlSchema()
        {
            return CreateMessageFrom("InvalidRequestChangeOfSupplier.xml");
        }

        private static Stream CreateMessage()
        {
            return CreateMessageFrom("ValidRequestChangeOfSupplier.xml");
        }

        private static Stream CreateMessageWithDuplicateTransactionIds()
        {
            return CreateMessageFrom("RequestChangeOfSupplierWithDuplicateTransactionIds.xml");
        }

        private static Stream CreateMessageFrom(string xmlFile)
        {
            using var fileReader = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
            var messageStream = new MemoryStream();
            fileReader.CopyTo(messageStream);
            messageStream.Position = 0;
            return messageStream;
        }

        private Task<Result> ReceiveRequestChangeOfSupplierMessage(Stream message, string version = "1.0")
        {
            return CreateMessageReceiver().ReceiveAsync(message, "requestchangeofsupplier", version);
        }

        private MessageReceiver CreateMessageReceiver()
        {
            var messageReceiver = new MessageReceiver(MessageIdRegistry, _marketActivityRecordForwarderSpy, _transactionIdsStub, new SchemaProvider(new SchemaStore()));
            return messageReceiver;
        }
    }
}
