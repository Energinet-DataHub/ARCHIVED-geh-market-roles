using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using B2B.CimMessageAdapter;
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
            Assert.Equal("12345699", activityRecord.MRid);
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
            var messageReceiver = new MessageReceiver(_messageIdsStub, _marketActivityRecordForwarderStub, _transactionIdsStub);
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

    public class MessageReceiver
    {
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIds _messageIds;
        private readonly IMarketActivityRecordForwarder _marketActivityRecordForwarder;
        private readonly ITransactionIds _transactionIds;
        private bool _hasInvalidHeaderValues;

        public MessageReceiver(IMessageIds messageIds, IMarketActivityRecordForwarder marketActivityRecordForwarder, ITransactionIds transactionIds)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _marketActivityRecordForwarder = marketActivityRecordForwarder ?? throw new ArgumentNullException(nameof(marketActivityRecordForwarder));
            _transactionIds = transactionIds;
        }

        public async Task<Result> ReceiveAsync(Stream message, string businessProcessType, string version)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var xmlSchema = await GetSchemaAsync(businessProcessType, version).ConfigureAwait(true);
            if (xmlSchema is null)
            {
                return Result.Failure(new ValidationError(
                    $"Schema version {version} for business process type {businessProcessType} does not exist."));
            }

            _hasInvalidHeaderValues = false;
            using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
            {
                try
                {
                    await HandleMessageHeaderValues(reader).ConfigureAwait(false);
                    await HandleMarketActivityRecordsAsync(reader).ConfigureAwait(false);
                }
                catch (XmlException exception)
                {
                    return Result.Failure(new ValidationError(exception.Message));
                }
            }

            if (_hasInvalidHeaderValues == false)
            {
                await _marketActivityRecordForwarder.CommitAsync().ConfigureAwait(false);
            }
            return _errors.Count == 0 ? Result.Succeeded() : Result.Failure(_errors.ToArray());
        }

        private async Task HandleMarketActivityRecordsAsync(XmlReader reader)
        {
            while (await reader.ReadAsync())
            {
                if (reader.NodeType != XmlNodeType.Element || !reader.LocalName.Equals("MktActivityRecord")) continue;
                await foreach (var marketActivityRecord in ExtractFromAsync(reader))
                {
                    if (await CheckTransactionIdAsync(marketActivityRecord.MRid).ConfigureAwait(false) == false)
                    {
                        _errors.Add(new ValidationError(
                            $"Transaction id '{marketActivityRecord.MRid}' is not unique and will not be processed."));
                    }
                    else
                    {
                        await StoreActivityRecordAsync(marketActivityRecord).ConfigureAwait(false);
                    }
                }
            }
        }

        private async IAsyncEnumerable<MarketActivityRecord> ExtractFromAsync(XmlReader reader)
        {
            var mRID = string.Empty;
            var marketEvaluationPointmRID = string.Empty;
            var energySupplierMarketParticipantmRID = string.Empty;
            var balanceResponsiblePartyMarketParticipantmRID = string.Empty;
            var customerMarketParticipantmRID = string.Empty;
            var customerMarketParticipantname = string.Empty;
            var startDateAndOrTimedateTime = string.Empty;

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.EndElement &&
                    reader.LocalName.Equals("MktActivityRecord"))
                {
                    if (reader.SchemaInfo.Validity == XmlSchemaValidity.Invalid)
                    {
                        continue;
                    }

                    yield return new MarketActivityRecord()
                    {
                        MRid = mRID,
                        CustomerMarketParticipantName = customerMarketParticipantname,
                        CustomerMarketParticipantmRID = customerMarketParticipantmRID,
                        MarketEvaluationPointmRID = marketEvaluationPointmRID,
                        EnergySupplierMarketParticipantmRID = energySupplierMarketParticipantmRID,
                        StartDateAndOrTimeDateTime = startDateAndOrTimedateTime,
                        BalanceResponsiblePartyMarketParticipantmRID =
                            balanceResponsiblePartyMarketParticipantmRID,
                    };
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    TryExtractValueFrom("mRID", reader, ref mRID);
                    TryExtractValueFrom("marketEvaluationPoint.mRID", reader, ref marketEvaluationPointmRID);
                    TryExtractValueFrom("marketEvaluationPoint.energySupplier_MarketParticipant.mRID", reader, ref energySupplierMarketParticipantmRID);
                    TryExtractValueFrom("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID", reader, ref balanceResponsiblePartyMarketParticipantmRID);
                    TryExtractValueFrom("marketEvaluationPoint.customer_MarketParticipant.mRID", reader, ref customerMarketParticipantmRID);
                    TryExtractValueFrom("marketEvaluationPoint.customer_MarketParticipant.name", reader, ref customerMarketParticipantname);
                    TryExtractValueFrom("start_DateAndOrTime.dateTime", reader, ref startDateAndOrTimedateTime);
                }
            }
        }

        private void TryExtractValueFrom(string elementName, XmlReader reader, ref string variable)
        {
            if (reader.LocalName.Equals(elementName))
            {
                variable = reader.ReadElementString();
            }
        }

        private async Task HandleMessageHeaderValues(XmlReader reader)
        {
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.LocalName.Equals("RequestChangeOfSupplier_MarketDocument"))
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.LocalName.Equals("mRID"))
                        {
                            var messageId = reader.ReadElementString();
                            var messageIdIsUnique = await CheckMessageIdAsync(messageId);
                            if (messageIdIsUnique == false)
                            {
                                _errors.Add(new DuplicateId($"Message id '{messageId}' is not unique"));
                                _hasInvalidHeaderValues = true;
                            }

                            break;
                        }
                    }

                    break;
                }
            }
        }

        private Task<bool> CheckTransactionIdAsync(string transactionId)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));
            return _transactionIds.TryStoreAsync(transactionId);
        }

        private Task StoreActivityRecordAsync(MarketActivityRecord marketActivityRecord)
        {
            return _marketActivityRecordForwarder.AddAsync(marketActivityRecord);
        }

        private Task<bool> CheckMessageIdAsync(string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            return _messageIds.TryStoreAsync(messageId);
        }

        private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
            };

            settings.Schemas.Add(xmlSchema);
            settings.ValidationEventHandler += OnValidationError;
            return settings;
        }

        private static Task<XmlSchema?> GetSchemaAsync(string businessProcessType, string version)
        {
            var schemas = new Dictionary<KeyValuePair<string, string>, string>()
            {
                {
                    new KeyValuePair<string, string>("requestchangeofsupplier", "1.0"),
                    "urn-ediel-org-structure-requestchangeofsupplier-0-1.xsd"
                }
            };

            if (schemas.TryGetValue(new KeyValuePair<string, string>(businessProcessType, version),
                    out var schemaName) == false)
            {
                return Task.FromResult(default(XmlSchema));
            }

            return LoadSchemaWithDependentSchemasAsync(schemaName);
        }

        private static async Task<XmlSchema> LoadSchemaWithDependentSchemasAsync(string location)
        {
            using var reader = new XmlTextReader(location);
            var xmlSchema = XmlSchema.Read(reader, null);

            foreach (XmlSchemaExternal external in xmlSchema.Includes)
            {
                if (external.SchemaLocation == null)
                {
                    continue;
                }

                external.Schema =
                    await LoadSchemaWithDependentSchemasAsync(external.SchemaLocation).ConfigureAwait(false);
            }

            return xmlSchema;
        }

        private void OnValidationError(object? sender, ValidationEventArgs arguments)
        {
            var message =
                $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
            _errors.Add(new ValidationError(message));
        }
    }
}
