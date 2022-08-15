using System;
using System.Threading.Tasks;
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;

namespace Messaging.Application.Common;

public class JsonHeaderWriter : IHeaderWriter
{
    private readonly JsonTextWriter _writer;

    public JsonHeaderWriter(JsonTextWriter writer)
    {
        _writer = writer;
    }

    public async Task WriteAsync(MessageHeader messageHeader, DocumentDetails documentDetails)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (documentDetails == null) throw new ArgumentNullException(nameof(documentDetails));
        _writer.Formatting = Newtonsoft.Json.Formatting.Indented;

        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync(documentDetails.Type).ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("mRID").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.MessageId).ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("businessSector.type").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync("23").ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("createdDateTime").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.TimeStamp.ToString()).ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("process.processType").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.ProcessType).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("reason.code").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.ReasonCode).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("receiver_MarketParticipant.mRID").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("codingScheme").ConfigureAwait(false);
        await _writer.WriteValueAsync("A10").ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.ReceiverId).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("receiver_MarketParticipant.marketRole.type").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.ReceiverRole).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("sender_MarketParticipant.mRID").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("codingScheme").ConfigureAwait(false);
        await _writer.WriteValueAsync("A10").ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.SenderId).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("sender_MarketParticipant.marketRole.type").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(messageHeader.SenderRole).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);

        await _writer.WritePropertyNameAsync("type").ConfigureAwait(false);
        await _writer.WriteStartObjectAsync().ConfigureAwait(false);
        await _writer.WritePropertyNameAsync("value").ConfigureAwait(false);
        await _writer.WriteValueAsync(documentDetails.TypeCode).ConfigureAwait(false);
        await _writer.WriteEndObjectAsync().ConfigureAwait(false);
    }
}
