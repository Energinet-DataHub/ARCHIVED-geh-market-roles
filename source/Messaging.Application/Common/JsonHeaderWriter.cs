using System;
using System.Threading.Tasks;
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Messaging.Application.Common;

public class JsonHeaderWriter : IHeaderWriter
{
    private readonly JsonTextWriter _writer;

    public JsonHeaderWriter(JsonTextWriter writer)
    {
        _writer = writer;
    }

    public Task WriteAsync(MessageHeader messageHeader, DocumentDetails documentDetails)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (documentDetails == null) throw new ArgumentNullException(nameof(documentDetails));
        _writer.Formatting = Newtonsoft.Json.Formatting.Indented;

        _writer.WriteStartObject();
        _writer.WritePropertyName(documentDetails.Type);
        _writer.WriteStartObject();
        _writer.WritePropertyName("mRID");
        _writer.WriteValue(messageHeader.MessageId);

        _writer.WritePropertyName("businessSector.type");
        _writer.WriteStartObject();
        _writer.WritePropertyName("value");
        _writer.WriteValue("23");
        _writer.WriteEndObject();

        _writer.WritePropertyName("createdDateTime");
        _writer.WriteValue(messageHeader.TimeStamp.ToString());

        _writer.WritePropertyName("process.processType");
        _writer.WriteStartObject();
        _writer.WritePropertyName("value");
        _writer.WriteValue(messageHeader.ProcessType);
        _writer.WriteEndObject();

        _writer.WritePropertyName("reason.code");
        _writer.WriteStartObject();
        _writer.WritePropertyName("value");
        _writer.WriteValue(messageHeader.ReasonCode);
        _writer.WriteEndObject();

        _writer.WritePropertyName("receiver_MarketParticipant.mRID");
        _writer.WriteStartObject();
        _writer.WritePropertyName("codingScheme");
        _writer.WriteValue("A10");
        _writer.WritePropertyName("value");
        _writer.WriteValue(messageHeader.ReceiverId);
        _writer.WriteEndObject();

        _writer.WritePropertyName("receiver_MarketParticipant.marketRole.type");
        _writer.WriteStartObject();
        _writer.WritePropertyName("value");
        _writer.WriteValue(messageHeader.ReceiverRole);
        _writer.WriteEndObject();

        _writer.WritePropertyName("sender_MarketParticipant.mRID");
        _writer.WriteStartObject();
        _writer.WritePropertyName("codingScheme");
        _writer.WriteValue("A10");
        _writer.WritePropertyName("value");
        _writer.WriteValue(messageHeader.SenderId);
        _writer.WriteEndObject();

        _writer.WritePropertyName("sender_MarketParticipant.marketRole.type");
        _writer.WriteStartObject();
        _writer.WritePropertyName("value");
        _writer.WriteValue(messageHeader.SenderRole);
        _writer.WriteEndObject();

        _writer.WritePropertyName("type");
        _writer.WriteStartObject();
        _writer.WritePropertyName("value");
        _writer.WriteValue(documentDetails.TypeCode);
        _writer.WriteEndObject();

        return Task.CompletedTask;
    }
}
