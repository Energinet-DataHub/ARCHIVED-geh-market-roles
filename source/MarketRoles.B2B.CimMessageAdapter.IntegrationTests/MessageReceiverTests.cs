using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
#pragma warning disable
    public class MessageReceiverTests
    {
        [Fact]
        public async Task Message_must_be_stored()
        {
            var message = new RawMessage("xml");
            var messageStore = new MessageStore();
            var messageReceiver = new MessageReceiver(messageStore);
            await messageReceiver.Receive(message).ConfigureAwait(false);

            Assert.Contains(message, messageStore.Messages);
        }
    }

    public class RawMessage
    {
        private readonly string _xml;

        public RawMessage(string xml)
        {
            _xml = xml;
        }
    }

    public class MessageStore
    {
        public List<RawMessage> Messages { get; } = new();

        public Task SaveAsync(RawMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Messages.Add(message);
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

        public Task Receive(RawMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            return _storage.SaveAsync(message);
        }
    }
}
