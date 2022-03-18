using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
#pragma warning disable
    public class MessageReceiverTests
    {
        private readonly MessageIdStore _messageIdStore = new();
        private readonly ActivityRecords _activityRecords;

        public MessageReceiverTests()
        {
            _activityRecords = new();
        }

        [Fact]
        public async Task Message_must_be_valid_xml()
        {
            var message = CreateMessageWithInvalidXmlStructure();
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message, "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Message_must_conform_to_xml_schema()
        {
            var message = CreateMessageNotConformingToXmlSchema();
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message, "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_does_not_exist()
        {
            var message = CreateMessage();
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message, "requestchangeofsupplier", "non_existing_version")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Return_failure_if_message_id_is_not_unique()
        {
            await CreateMessageReceiver().ReceiveAsync(CreateMessage(), "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            var messageReceiver = CreateMessageReceiver();
            var result = await messageReceiver.ReceiveAsync(CreateMessage(), "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            var messageReceiver = CreateMessageReceiver();
            await messageReceiver
                .ReceiveAsync(CreateMessageNotConformingToXmlSchema(), "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            Assert.Single(_activityRecords.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_are_not_committed_to_if_any_message_header_values_are_invalid()
        {
            await CreateMessageReceiver().ReceiveAsync(CreateMessage(), "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            var messageReceiver = CreateMessageReceiver();
            await messageReceiver.ReceiveAsync(CreateMessage(), "requestchangeofsupplier", "1.0")
                .ConfigureAwait(false);

            Assert.Empty(_activityRecords.CommittedItems);
        }



        private MessageReceiver CreateMessageReceiver()
        {
            var messageReceiver = new MessageReceiver(_messageIdStore, _activityRecords);
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

        private Stream CreateMessageFrom(string xmlFile)
        {
            var messageStream = new MemoryStream();
            using var fileReader = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
            fileReader.CopyTo(messageStream);
            messageStream.Position = 0;
            return messageStream;
        }
    }

    public class ActivityRecords
    {
        private readonly List<ActivityRecord> _uncommittedItems = new();
        private readonly List<ActivityRecord> _committedItems = new();
        public IReadOnlyCollection<ActivityRecord> CommittedItems => _committedItems.AsReadOnly();

        public async Task AddAsync(ActivityRecord activityRecord)
        {
            _committedItems.Clear();
            _uncommittedItems.Add(activityRecord);
        }

        public Task CommitAsync()
        {
            _committedItems.Clear();
            _committedItems.AddRange(_uncommittedItems);
            _uncommittedItems.Clear();
            return Task.CompletedTask;
        }
    }

    public class ActivityRecord
    {
        public string mRid { get; set; }
    }

    public class MessageReceiver
    {
        private readonly List<Error> _errors = new();
        private readonly MessageIdStore _messageIds;
        private readonly ActivityRecords _activityRecords;

        public MessageReceiver(MessageIdStore messageIds, ActivityRecords activityRecords)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _activityRecords = activityRecords ?? throw new ArgumentNullException(nameof(activityRecords));
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
                                        _errors.Add(new Error($"Message id '{messageId}' is not unique"));
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
                                        var activityRecord = new ActivityRecord() { mRid = mRID, };
                                        await StoreActivityRecordAsync(activityRecord).ConfigureAwait(false);
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
                await _activityRecords.CommitAsync().ConfigureAwait(false);
            }
            return _errors.Count == 0 ? Result.Succeeded() : Result.Failure(_errors.ToArray());
        }

        private Task StoreActivityRecordAsync(ActivityRecord activityRecord)
        {
            return _activityRecords.AddAsync(activityRecord);
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
}
