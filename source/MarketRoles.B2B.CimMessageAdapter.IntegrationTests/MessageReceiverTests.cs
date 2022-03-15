using System;
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

        public MessageReceiver(MessageStore storage)
        {
            _storage = storage;
        }

        public async Task<Result> ReceiveAsync(Stream message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            await _storage.SaveAsync(message);

            var settings = new XmlReaderSettings();
            settings.Async = true;
            settings.ValidationType = ValidationType.Schema;
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
                    return Result.Failure();
                }
            }

            return Result.Succeeded();
        }

        private void OnValidationError(object? sender, ValidationEventArgs arguments)
        {
        }
    }

    public class Result
    {
        private Result(bool success)
        {
            Success = success;
        }
        public bool Success { get; } = true;

        public static Result Failure()
        {
            return new Result(false);
        }

        public static Result Succeeded()
        {
            return new Result(true);
        }
    }
}
