using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
#pragma warning disable
    public class MessageReceiverTests
    {
        [Fact]
        public async Task Message_must_be_valid_xml()
        {
            var message = CreateMessage("this is not valid XML");
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message, "requestchangeofsupplier", "1.0").ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Message_must_conform_to_xml_schema()
        {
            var message = CreateMessageFrom("InvalidMessageContainingTwoErrors.xml");
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message, "requestchangeofsupplier", "1.0").ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_does_not_exist()
        {
            var message = CreateMessage("this is not valid XML");
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message, "requestchangeofsupplier", "non_existing_version").ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        private static MessageReceiver CreateMessageReceiver()
        {
            var messageReceiver = new MessageReceiver();
            return messageReceiver;
        }

        private Stream CreateMessage(string xml)
        {
            var messageStream = new MemoryStream();
            var writer = new StreamWriter(messageStream);
            writer.Write(xml);
            writer.Flush();
            messageStream.Position = 0;
            return messageStream;
        }

        private Stream CreateMessageFrom(string xmlFile)
        {
            var messageStream = new MemoryStream();
            var fileReader = new FileStream(xmlFile, FileMode.Open);
            fileReader.CopyTo(messageStream);
            messageStream.Position = 0;
            return messageStream;
        }
    }

    public class MessageReceiver
    {
        private readonly List<Error> _errors = new();

        public MessageReceiver()
        {
        }

        public async Task<Result> ReceiveAsync(Stream message, string businessProcessType, string version)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var xmlSchema = GetSchema(businessProcessType, version);
            if (xmlSchema is null)
            {
                return Result.Failure(new Error(
                    $"Schema version {version} for business process type {businessProcessType} does not exist."));
            }
            using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
            {
                try
                {
                    await reader.MoveToContentAsync();
                    while (await reader.ReadAsync())
                    {
                    }
                }
                catch (XmlException exception)
                {
                    return Result.Failure(new Error(exception.Message));
                }
            }

            return _errors.Count == 0 ? Result.Succeeded() : Result.Failure(_errors.ToArray());
        }

        private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
            };
            settings.Schemas.Add(xmlSchema);
            settings.ValidationEventHandler += OnValidationError;

            return settings;
        }

        private static XmlSchema? GetSchema(string businessProcessType, string version)
        {
            var schemas = new Dictionary<KeyValuePair<string, string>, string>()
                {
                    { new KeyValuePair<string, string>("requestchangeofsupplier", "1.0"), "schema.xsd" }
                };

            if (schemas.TryGetValue(new KeyValuePair<string, string>(businessProcessType, version), out var schemaName) == false)
            {
                return null;
            }

            using var schemaReader = new XmlTextReader(schemaName);
            return XmlSchema.Read(schemaReader, (sender, args) => throw new XmlSchemaException("Invalid XML schema"));
        }

        private void OnValidationError(object? sender, ValidationEventArgs arguments)
        {
            var message =
                $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
            _errors.Add(new Error(message));
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
