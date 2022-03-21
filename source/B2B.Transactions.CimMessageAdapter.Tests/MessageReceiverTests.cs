using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B2B.CimMessageAdapter;
using MarketRoles.B2B.CimMessageAdapter.IntegrationTests.Stubs;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
#pragma warning disable
    public class MessageReceiverTests
    {
        private readonly MessageIdsStub _messageIdsStub = new();
        private MarketActivityRecordForwarderStub _marketActivityRecordForwarderStub;
        private readonly TransactionIdsStub _transactionIdsStub = new();

        public MessageReceiverTests()
        {
        }

        [Fact]
        public async Task Message_must_be_valid_xml()
        {
            var result = await ReceiveRequestChangeOfSupplierMessage(CreateMessageWithInvalidXmlStructure()).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Message_must_conform_to_xml_schema()
        {
            var message = CreateMessageNotConformingToXmlSchema();
            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_does_not_exist()
        {
            var message = CreateMessage();

            var result = await ReceiveRequestChangeOfSupplierMessage(message, "non_existing_version")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Return_failure_if_message_id_is_not_unique()
        {
            await ReceiveRequestChangeOfSupplierMessage(CreateMessage()).ConfigureAwait(false);

            var result = await ReceiveRequestChangeOfSupplierMessage(CreateMessage()).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, error => error is DuplicateId);
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            await ReceiveRequestChangeOfSupplierMessage(CreateMessageNotConformingToXmlSchema())
                .ConfigureAwait(false);

            var activityRecord = _marketActivityRecordForwarderStub.CommittedItems.FirstOrDefault();
            Assert.NotNull(activityRecord);
            Assert.Equal("12345699", activityRecord.MrId);
            Assert.Equal("579999993331812345", activityRecord.MarketEvaluationPointmRID);
            Assert.Equal("5799999933318", activityRecord.EnergySupplierMarketParticipantmRID);
            Assert.Equal("5799999933340", activityRecord.BalanceResponsiblePartyMarketParticipantmRID);
            Assert.Equal("0801741527", activityRecord.CustomerMarketParticipantmRID);
            Assert.Equal("Jan Hansen", activityRecord.CustomerMarketParticipantName);
            Assert.Equal("2022-09-07T22:00:00Z", activityRecord.StartDateAndOrTimeDateTime);
        }

        [Fact]
        public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
        {
            await ReceiveRequestChangeOfSupplierMessage(CreateMessage())
                .ConfigureAwait(false);

            await ReceiveRequestChangeOfSupplierMessage(CreateMessage())
                .ConfigureAwait(false);

            Assert.Empty(_marketActivityRecordForwarderStub.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_must_have_unique_transaction_ids()
        {
            await ReceiveRequestChangeOfSupplierMessage(CreateMessageWithDuplicateTransactionIds())
                .ConfigureAwait(false);

            Assert.Single(_marketActivityRecordForwarderStub.CommittedItems);
        }


        private Task<Result> ReceiveRequestChangeOfSupplierMessage(Stream message, string version = "1.0")
        {
            return CreateMessageReceiver().ReceiveAsync(message, "requestchangeofsupplier", version);
        }


        private MessageReceiver CreateMessageReceiver()
        {
            _marketActivityRecordForwarderStub = new MarketActivityRecordForwarderStub();
            var messageReceiver = new MessageReceiver(_messageIdsStub, _marketActivityRecordForwarderStub, _transactionIdsStub, new SchemaProviderStub());
            return messageReceiver;
        }

        private Stream CreateMessageWithInvalidXmlStructure()
        {
            var messageStream = new MemoryStream();
            var writer = new StreamWriter(messageStream);
            writer.Write("This is not XML");
            writer.Flush();
            messageStream.Position = 0;
            return messageStream;
        }

        private Stream CreateMessageNotConformingToXmlSchema()
        {
            return CreateMessageFrom("InvalidRequestChangeOfSupplier.xml");
        }

        private Stream CreateMessage()
        {
            return CreateMessageFrom("ValidRequestChangeOfSupplier.xml");
        }

        private Stream CreateMessageWithDuplicateTransactionIds()
        {
            return CreateMessageFrom("RequestChangeOfSupplierWithDuplicateTransactionIds.xml");
        }

        private Stream CreateMessageFrom(string xmlFile)
        {
            var messageStream = new MemoryStream();
            using var fileReader = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
            fileReader.CopyTo(messageStream);
            messageStream.Position = 0;
            return messageStream;
        }
    }
}
