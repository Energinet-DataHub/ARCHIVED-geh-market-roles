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
using System.Threading.Tasks;
using System.Xml;
using Messaging.Application.Common;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmChangeOfSupplierDocumentWriter : DocumentWriter
{
    public ConfirmChangeOfSupplierDocumentWriter(IMarketActivityRecordParser parser)
    : base(
        new DocumentDetails(
            "ConfirmRequestChangeOfSupplier_MarketDocument",
            "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd",
            "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1",
            "cim",
            "E44"),
        parser)
    {
    }

    public override Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));

        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
        }

        return Task.CompletedTask;
    }

    public override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter xmlWriter)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (xmlWriter == null) throw new ArgumentNullException(nameof(xmlWriter));
        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
            await xmlWriter.WriteStartElementAsync(DocumentDetails.Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await xmlWriter.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, marketActivityRecord.Id.ToString())
                .ConfigureAwait(false);
            await xmlWriter.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "originalTransactionIDReference_MktActivityRecord.mRID",
                null,
                marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
            await xmlWriter.WriteStartElementAsync(DocumentDetails.Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
            await xmlWriter.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            xmlWriter.WriteValue(marketActivityRecord.MarketEvaluationPointId);
            await xmlWriter.WriteEndElementAsync().ConfigureAwait(false);
            await xmlWriter.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    protected override Task WriteHeaderAsync(MessageHeader header, DocumentDetails documentDetails, XmlWriter writer)
    {
        return new XmlHeaderWriter(writer).WriteAsync(header, documentDetails);
    }

    protected override Task WriteHeaderAsync(MessageHeader header, DocumentDetails documentDetails)
    {
        return new JsonHeaderWriter().WriteAsync(header, documentDetails);
    }
}
