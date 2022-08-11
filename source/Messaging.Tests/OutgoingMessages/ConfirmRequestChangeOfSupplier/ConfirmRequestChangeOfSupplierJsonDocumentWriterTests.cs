﻿using System;
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
using Xunit;

namespace Messaging.Tests.OutgoingMessages.ConfirmRequestChangeOfSupplier;

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
        var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now(), "A01");
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

        AssertMessage(message, header, marketActivityRecords);
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

    private static void AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        var json = StreamToJson(message);
        var firstChild = json.GetValue("MktActivityRecord", StringComparison.OrdinalIgnoreCase)[0];
        var secondChild = json.GetValue("MktActivityRecord", StringComparison.OrdinalIgnoreCase)[1];

        Assert.Equal(2, json.GetValue("MktActivityRecord", StringComparison.OrdinalIgnoreCase).Count());
        Assert.Equal("mrid1", firstChild.Value<string>("mRID"));
        Assert.Equal("OriginalTransactionId", firstChild.Value<string>("originalTransactionIDReference_MktActivityRecord.mRID"));
        Assert.Equal("FakeMarketEvaluationPointId", firstChild.First.Next.First.Value<string>("value"));
    }
}
