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

            var result = await messageReceiver.ReceiveAsync(message).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Message_must_conform_to_xml_schema()
        {
            var message = CreateMessageFrom("InvalidMessageContainingTwoErrors.xml");
            var messageReceiver = CreateMessageReceiver();

            var result = await messageReceiver.ReceiveAsync(message).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Equal(2, result.Errors.Count);
        }

        private static MessageReceiver CreateMessageReceiver()
        {
            var messageStore = new MessageStore();
            var messageReceiver = new MessageReceiver(messageStore);
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

    public class MessageStore
    {
        public Task SaveAsync(Stream message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            return Task.CompletedTask;
        }
    }

    public class MessageReceiver
    {
        private readonly MessageStore _storage;
        private readonly List<Error> _errors = new();

        public MessageReceiver(MessageStore storage)
        {
            _storage = storage;
        }

        public async Task<Result> ReceiveAsync(Stream message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            await _storage.SaveAsync(message);

            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
            };

            var schemaReader = new XmlTextReader("Schema.xsd");
            var schema = XmlSchema.Read(schemaReader, OnValidationError);
            settings.Schemas.Add(schema);

            settings.ValidationEventHandler += OnValidationError;
            using (var reader = XmlReader.Create(message, settings))
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
