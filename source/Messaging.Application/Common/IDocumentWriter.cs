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

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.MessageHub.Model.Model;
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;

namespace Messaging.Application.Common;

/// <summary>
/// Document writer interface
/// </summary>
public interface IDocumentWriter
{
    /// <summary>
    /// Object containing document details
    /// </summary>
    DocumentDetails DocumentDetails { get; }

    /// <summary>
    /// Write a document async
    /// </summary>
    /// <param name="header"></param>
    /// <param name="marketActivityRecords"></param>
    /// <param name="responseFormat"></param>
    /// <param name="responseVersion"></param>
    /// <returns><see cref="Task"/></returns>
    Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords, ResponseFormat responseFormat, double responseVersion);

    /// <summary>
    /// Document type handler
    /// </summary>
    /// <param name="documentType"></param>
    /// <returns><see cref="bool"/></returns>
    bool HandlesDocumentType(string documentType);

    /// <summary>
    /// Async writer for json documents
    /// </summary>
    /// <param name="marketActivityPayloads"></param>
    /// <param name="jsonTextWriter"></param>
    /// <returns><see cref="Task"/></returns>
    Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, JsonTextWriter jsonTextWriter);

    /// <summary>
    /// Async writer for xml documents
    /// </summary>
    /// <param name="marketActivityPayloads"></param>
    /// <param name="xmlWriter"></param>
    /// <returns><see cref="Task"/></returns>
    Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter xmlWriter);

    /// <summary>
    /// Market activity parser
    /// </summary>
    /// <param name="payloads"></param>
    /// <typeparam name="TMarketActivityRecord">1234</typeparam>
    /// <returns><see cref="IReadOnlyCollection{T}"/></returns>
    IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads);
}
