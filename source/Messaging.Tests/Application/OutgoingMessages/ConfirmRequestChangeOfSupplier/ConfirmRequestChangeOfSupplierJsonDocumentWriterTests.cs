using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Common;
using Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Extensions;
using Xunit;

namespace Messaging.Tests.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmRequestChangeOfSupplierJsonDocumentWriterTests
{
    private readonly SystemDateTimeProvider _systemDateTimeProvider;
    private readonly DocumentWriter _documentWriter;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;

    public ConfirmRequestChangeOfSupplierJsonDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
        _documentWriter = new ConfirmChangeOfSupplierDocumentWriter(_marketActivityRecordParser);
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", "messageID", _systemDateTimeProvider.Now(), "A01");
        var documentDetails = new DocumentDetails("ConfirmRequestChangeOfSupplier_MarketDocument", null, null, null, typeCode: "E44");
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            new("mrid1", "OriginalTransactionId", "FakeMarketEvaluationPointId"),
            new("mrid2", "FakeTransactionId", "FakeMarketEvaluationPointId"),
        };

        var message = await _documentWriter.WriteAsync(
            header,
            marketActivityRecords
            .Select(record => _marketActivityRecordParser.From(record)).ToList(),
            CimType.Json).ConfigureAwait(false);

        AssertMessage(message, header, documentDetails);
    }

    private static JObject StreamToJson(Stream stream)
    {
        stream.Position = 0;
        var serializer = new JsonSerializer();
        var sr = new StreamReader(stream);
        using var jtr = new JsonTextReader(sr);
        var json = serializer.Deserialize<JObject>(jtr);

        return json;
    }

    private static void AssertMessage(Stream message, MessageHeader header, DocumentDetails details)
    {
        var json = StreamToJson(message);
        AssertHeader(header, details, json);
        AssertMarketActivityRecord(json);
    }

    private static void AssertHeader(MessageHeader header, DocumentDetails details, JObject json)
    {
        var document = json.GetValue(
            "ConfirmRequestChangeOfSupplier_MarketDocument",
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal("messageID", document.Value<string>("mRID"));
        Assert.Equal("23", document.Value<JToken>("businessSector.type").First.First);
        var headerDateTime = TruncateMilliseconds(header.TimeStamp.ToDateTimeUtc());
        var documentDateTime = TruncateMilliseconds(document.Value<DateTime>("createdDateTime"));
        Assert.Equal(headerDateTime, documentDateTime);
        Assert.Equal(header.ProcessType, document.Value<JToken>("process.processType").First.First);
        Assert.Equal(header.ReasonCode, document.Value<JToken>("reason.code").First.First);
        Assert.Equal(header.ReceiverId, document.Value<JToken>("receiver_MarketParticipant.mRID").Value<string>("value"));
        Assert.Equal(header.ReceiverRole, document.Value<JToken>("receiver_MarketParticipant.marketRole.type").First.First);
        Assert.Equal(header.SenderId, document.Value<JToken>("sender_MarketParticipant.mRID").Value<string>("value"));
        Assert.Equal(header.SenderRole, document.Value<JToken>("sender_MarketParticipant.marketRole.type").First.First);
        Assert.Equal(details.TypeCode, document.Value<JToken>("type").Value<string>("value"));
    }

    private static void AssertMarketActivityRecord(JObject json)
    {
        var marketActivityRecords =
            json.GetValue("ConfirmRequestChangeOfSupplier_MarketDocument", StringComparison.OrdinalIgnoreCase)
                .Value<JArray>("MktActivityRecord").ToList();
        var firstChild = marketActivityRecords[0];
        var secondChild = marketActivityRecords[1];

        Assert.Equal(2, marketActivityRecords.Count);
        Assert.Equal("mrid1", firstChild.Value<string>("mRID"));
        Assert.Equal(
            "OriginalTransactionId",
            firstChild.Value<string>("originalTransactionIDReference_MktActivityRecord.mRID"));
        Assert.Equal("FakeMarketEvaluationPointId", firstChild.First.Next.First.Value<string>("value"));

        Assert.Equal("mrid2", secondChild.Value<string>("mRID"));
        Assert.Equal(
            "FakeTransactionId",
            secondChild.Value<string>("originalTransactionIDReference_MktActivityRecord.mRID"));
        Assert.Equal("FakeMarketEvaluationPointId", secondChild.First.Next.First.Value<string>("value"));
    }

    private static DateTime TruncateMilliseconds(DateTime time)
    {
        return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
    }
}
