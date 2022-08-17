﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.MessageHub.Model.Model;
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Messaging.Application.Common;

public abstract class DocumentWriter : IDocumentWriter
{
    private readonly DocumentDetails _documentDetails;
    private readonly IMarketActivityRecordParser _parser;

    protected DocumentWriter(DocumentDetails documentDetails, IMarketActivityRecordParser parser)
    {
        _documentDetails = documentDetails;
        _parser = parser;
    }

    public DocumentDetails DocumentDetails => _documentDetails;

    public async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords, ResponseFormat responseFormat, double responseVersion)
    {
        if (responseFormat == ResponseFormat.Xml)
        {
            return await WriteXmlAsync(header, marketActivityRecords).ConfigureAwait(false);
        }
        else
        {
            return await WriteJsonAsync(header, marketActivityRecords).ConfigureAwait(false);
        }
    }

    public bool HandlesDocumentType(string documentType)
    {
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        return _documentDetails.Type[..documentType.Length].Equals(documentType, StringComparison.OrdinalIgnoreCase);
    }

    public abstract Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter xmlWriter);

    public abstract Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, JsonTextWriter jsonTextWriter);

    public IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads)
    {
        if (payloads == null) throw new ArgumentNullException(nameof(payloads));
        var marketActivityRecords = new List<TMarketActivityRecord>();
        foreach (var payload in payloads)
        {
            marketActivityRecords.Add(_parser.From<TMarketActivityRecord>(payload));
        }

        return marketActivityRecords;
    }

    protected Task WriteElementAsync(string name, string value, XmlWriter writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        return writer.WriteElementStringAsync(DocumentDetails.Prefix, name, null, value);
    }

    protected async Task WriteMridAsync(string localName, string id, string codingScheme, XmlWriter writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, localName, null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, codingScheme).ConfigureAwait(false);
        writer.WriteValue(id);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    protected abstract Task WriteHeaderAsync(MessageHeader header, DocumentDetails documentDetails, XmlWriter writer);

    protected abstract Task WriteHeaderAsync(MessageHeader header, DocumentDetails documentDetails, JsonTextWriter writer);

    private static async Task WriteEndAsync(XmlWriter writer)
    {
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        writer.Close();
    }

    private async Task<Stream> WriteXmlAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8, Async = true };
        var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, settings);
        await WriteHeaderAsync(header, _documentDetails, writer).ConfigureAwait(false);
        await WriteMarketActivityRecordsAsync(marketActivityRecords, writer).ConfigureAwait(false);
        await WriteEndAsync(writer).ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    private async Task<Stream> WriteJsonAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MemoryStream();
        var streamWriter = new StreamWriter(stream);
        using var writer = new JsonTextWriter(streamWriter);
        writer.Formatting = Formatting.Indented;
        await WriteHeaderAsync(header, _documentDetails, writer).ConfigureAwait(false);
        await WriteMarketActivityRecordsAsync(marketActivityRecords, writer).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        await streamWriter.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        var returnStream = new MemoryStream();
        await stream.CopyToAsync(returnStream).ConfigureAwait(false);

        return returnStream;
    }
}
