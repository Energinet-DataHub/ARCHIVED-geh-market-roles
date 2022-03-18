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
        private readonly MessageIdStore _messageIdStore = new();
        private ActivityRecordForwarder _activityRecordForwarder;
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
            Assert.Contains(result.Errors, error => error is DuplicateMessageId);
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            await ReceiveRequestChangeOfSupplierMessage(CreateMessageNotConformingToXmlSchema())
                .ConfigureAwait(false);

            var activityRecord = _activityRecordForwarder.CommittedItems.FirstOrDefault();
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

            Assert.Empty(_activityRecordForwarder.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_must_have_unique_transaction_ids()
        {
            await ReceiveRequestChangeOfSupplierMessage(CreateMessageWithDuplicateTransactionIds())
                .ConfigureAwait(false);

            Assert.Single(_activityRecordForwarder.CommittedItems);
        }


        private Task<Result> ReceiveRequestChangeOfSupplierMessage(Stream message, string version = "1.0")
        {
            return CreateMessageReceiver().ReceiveAsync(message, "requestchangeofsupplier", version);
        }


        private MessageReceiver CreateMessageReceiver()
        {
            _activityRecordForwarder = new ActivityRecordForwarder();
            var messageReceiver = new MessageReceiver(_messageIdStore, _activityRecordForwarder, _transactionIdsStub);
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

    public class ActivityRecordForwarder : IActivityRecordForwarder
    {
        private readonly List<MarketActivityRecord> _uncommittedItems = new();
        private readonly List<MarketActivityRecord> _committedItems = new();
        public IReadOnlyCollection<MarketActivityRecord> CommittedItems => _committedItems.AsReadOnly();

        public async Task AddAsync(MarketActivityRecord marketActivityRecord)
        {
            _committedItems.Clear();
            _uncommittedItems.Add(marketActivityRecord);
        }

        public Task CommitAsync()
        {
            _committedItems.Clear();
            _committedItems.AddRange(_uncommittedItems);
            _uncommittedItems.Clear();
            return Task.CompletedTask;
        }
    }

    public class MessageReceiver
    {
        private readonly List<Error> _errors = new();
        private readonly MessageIdStore _messageIds;
        private readonly ActivityRecordForwarder _activityRecordForwarder;
        private readonly TransactionIdsStub _transactionIdsStub;

        public MessageReceiver(MessageIdStore messageIds, ActivityRecordForwarder activityRecordForwarder, TransactionIdsStub transactionIdsStub)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _activityRecordForwarder = activityRecordForwarder ?? throw new ArgumentNullException(nameof(activityRecordForwarder));
            _transactionIdsStub = transactionIdsStub;
        }

        public async Task<Result> ReceiveAsync(Stream message, string businessProcessType, string version)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var xmlSchema = await GetSchemaAsync(businessProcessType, version).ConfigureAwait(true);
            if (xmlSchema is null)
            {
                return Result.Failure(new Error(
                    $"Schema version {version} for business process type {businessProcessType} does not exist."));
            }

            bool hasInvalidHeaderValues = false;
            using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
            {
                try
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
                                        _errors.Add(new DuplicateMessageId($"Message id '{messageId}' is not unique"));
                                        hasInvalidHeaderValues = true;
                                    }

                                    break;
                                }
                            }
                        }

                        if (reader.NodeType == XmlNodeType.Element &&
                            reader.LocalName.Equals("MktActivityRecord"))
                        {
                            string mRID = string.Empty;
                            string marketEvaluationPointmRID = string.Empty;
                            string energySupplierMarketParticipantmRID = string.Empty;
                            string balanceResponsiblePartyMarketParticipantmRID = string.Empty;
                            string customerMarketParticipantmRID = string.Empty;
                            string customerMarketParticipantname = string.Empty;
                            string startDateAndOrTimedateTime = string.Empty;

                            bool hasError = false;
                            while (await reader.ReadAsync())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement &&
                                    reader.LocalName.Equals("MktActivityRecord"))
                                {
                                    if (hasError == false)
                                    {
                                        var transactionId = mRID;
                                        var transactionIdIsUnique = await CheckTransactionIdAsync(transactionId);
                                        if (transactionIdIsUnique == false)
                                        {
                                            _errors.Add(new Error($"Transaction id '{ transactionId }' is not unique and will not be processed."));
                                        }
                                        else
                                        {
                                            var activityRecord = new MarketActivityRecord()
                                            {
                                                MRid = mRID,
                                                CustomerMarketParticipantName = customerMarketParticipantname,
                                                CustomerMarketParticipantmRID = customerMarketParticipantmRID,
                                                MarketEvaluationPointmRID = marketEvaluationPointmRID,
                                                EnergySupplierMarketParticipantmRID = energySupplierMarketParticipantmRID,
                                                StartDateAndOrTimeDateTime = startDateAndOrTimedateTime,
                                                BalanceResponsiblePartyMarketParticipantmRID = balanceResponsiblePartyMarketParticipantmRID,
                                            };
                                            await StoreActivityRecordAsync(activityRecord).ConfigureAwait(false);
                                        }
                                    }

                                    break;
                                }

                                if (reader.SchemaInfo.Validity == XmlSchemaValidity.Invalid)
                                {
                                    hasError = true;
                                }
                                else
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        if (reader.LocalName.Equals("mRID"))
                                        {
                                            mRID = reader.ReadElementString();
                                        }

                                        if (reader.LocalName.Equals("marketEvaluationPoint.mRID"))
                                        {
                                            marketEvaluationPointmRID = reader.ReadElementString();
                                        }

                                        if (reader.LocalName.Equals(
                                                "marketEvaluationPoint.energySupplier_MarketParticipant.mRID"))
                                        {
                                            energySupplierMarketParticipantmRID = reader.ReadElementString();
                                        }

                                        if (reader.LocalName.Equals(
                                                "marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID"))
                                        {
                                            balanceResponsiblePartyMarketParticipantmRID = reader.ReadElementString();
                                        }

                                        if (reader.LocalName.Equals(
                                                "marketEvaluationPoint.customer_MarketParticipant.mRID"))
                                        {
                                            customerMarketParticipantmRID = reader.ReadElementString();
                                        }

                                        if (reader.LocalName.Equals(
                                                "marketEvaluationPoint.customer_MarketParticipant.name"))
                                        {
                                            customerMarketParticipantname = reader.ReadElementString();
                                        }

                                        if (reader.LocalName.Equals("start_DateAndOrTime.dateTime"))
                                        {
                                            startDateAndOrTimedateTime = reader.ReadElementString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (XmlException exception)
                {
                    return Result.Failure(new Error(exception.Message));
                }
            }

            if (hasInvalidHeaderValues == false)
            {
                await _activityRecordForwarder.CommitAsync().ConfigureAwait(false);
            }
            return _errors.Count == 0 ? Result.Succeeded() : Result.Failure(_errors.ToArray());
        }

        private Task<bool> CheckTransactionIdAsync(string transactionId)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));
            return _transactionIdsStub.TryStoreAsync(transactionId);
        }

        private Task StoreActivityRecordAsync(MarketActivityRecord marketActivityRecord)
        {
            return _activityRecordForwarder.AddAsync(marketActivityRecord);
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
            _errors.Add(new Error(message));
        }
    }

    public class MessageIdStore
    {
        private readonly HashSet<string> _messageIds = new();

        public Task<bool> TryStoreAsync(string messageId)
        {
            return Task.FromResult(_messageIds.Add(messageId));
        }
    }

    public class Result
    {
        private Result()
        {
        }

        private Result(IReadOnlyCollection<Error> errors)
        {
            Errors = errors;
        }

        public bool Success => Errors.Count == 0;

        public IReadOnlyCollection<Error> Errors { get; } = new List<Error>();

        public static Result Failure(params Error[] errors)
        {
            return new Result(errors);
        }

        public static Result Succeeded()
        {
            return new Result();
        }
    }

    public class Error
    {
        public Error(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public class DuplicateMessageId : Error
    {
        public DuplicateMessageId(string message)
            : base(message)
        {
        }
    }
}
