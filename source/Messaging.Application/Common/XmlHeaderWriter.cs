// Copyright 2020 Energinet DataHub A/S
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
using System.Threading.Tasks;
using System.Xml;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.Common;

public class XmlHeaderWriter : IHeaderWriter
{
    private readonly XmlWriter _writer;

    public XmlHeaderWriter(XmlWriter writer)
    {
        _writer = writer;
    }

    public async Task WriteAsync(MessageHeader messageHeader, DocumentDetails documentDetails)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (documentDetails == null) throw new ArgumentNullException(nameof(documentDetails));

        await _writer.WriteStartDocumentAsync().ConfigureAwait(false);
        await _writer.WriteStartElementAsync(
            documentDetails.Prefix,
            documentDetails.Type,
            documentDetails.XmlNamespace).ConfigureAwait(false);
        await _writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance")
            .ConfigureAwait(false);
        await _writer.WriteAttributeStringAsync(
                "xsi",
                "schemaLocation",
                null,
                documentDetails.SchemaLocation)
            .ConfigureAwait(false);
        await _writer.WriteElementStringAsync(documentDetails.Prefix, "mRID", null, messageHeader.MessageId).ConfigureAwait(false);
        await _writer.WriteElementStringAsync(documentDetails.Prefix, "type", null, documentDetails.TypeCode ?? string.Empty).ConfigureAwait(false);
        await _writer.WriteElementStringAsync(documentDetails.Prefix, "process.processType", null, messageHeader.ProcessType)
            .ConfigureAwait(false);
        await _writer.WriteElementStringAsync(documentDetails.Prefix, "businessSector.type", null, "23").ConfigureAwait(false);

        await _writer.WriteStartElementAsync(documentDetails.Prefix, "sender_MarketParticipant.mRID", null).ConfigureAwait(false);
        await _writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
        _writer.WriteValue(messageHeader.SenderId);
        await _writer.WriteEndElementAsync().ConfigureAwait(false);

        await _writer.WriteElementStringAsync(documentDetails.Prefix, "sender_MarketParticipant.marketRole.type", null, "DDZ")
            .ConfigureAwait(false);

        await _writer.WriteStartElementAsync(documentDetails.Prefix, "receiver_MarketParticipant.mRID", null).ConfigureAwait(false);
        await _writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
        _writer.WriteValue(messageHeader.ReceiverId);
        await _writer.WriteEndElementAsync().ConfigureAwait(false);

        await _writer
            .WriteElementStringAsync(documentDetails.Prefix, "receiver_MarketParticipant.marketRole.type", null, messageHeader.ReceiverRole)
            .ConfigureAwait(false);
        await _writer.WriteElementStringAsync(documentDetails.Prefix, "createdDateTime", null, messageHeader.TimeStamp.ToString()).ConfigureAwait(false);
        if (messageHeader.ReasonCode is not null)
        {
            await _writer.WriteElementStringAsync(documentDetails.Prefix, "reason.code", null, messageHeader.ReasonCode).ConfigureAwait(false);
        }
    }
}
