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
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;

namespace Messaging.Application.Common;

/// <summary>
/// bla
/// </summary>
public interface IDocumentWriter
{
    /// <summary>
    /// bla
    /// </summary>
    DocumentDetails DocumentDetails { get; }

    /// <summary>
    /// blaaaaaaaaaa
    /// </summary>
    /// <param name="header"></param>
    /// <param name="marketActivityRecords"></param>
    /// <param name="cimType"></param>
    /// <returns>fsdfdsfd</returns>
    Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords, CimType cimType);

    /// <summary>
    /// bla bla bla
    /// </summary>
    /// <param name="documentType"></param>
    /// <returns>bla bla too</returns>
    bool HandlesDocumentType(string documentType);

    /// <summary>
    /// blalalal
    /// </summary>
    /// <param name="marketActivityPayloads"></param>
    /// <param name="jsonTextWriter"></param>
    /// <returns>fkdskfs</returns>
    Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, JsonTextWriter jsonTextWriter);

    /// <summary>
    /// mcmcmc
    /// </summary>
    /// <param name="marketActivityPayloads"></param>
    /// <param name="xmlWriter"></param>
    /// <returns>dnsdujsdf</returns>
    Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter xmlWriter);

    /// <summary>
    /// 123
    /// </summary>
    /// <param name="payloads"></param>
    /// <typeparam name="TMarketActivityRecord">1234</typeparam>
    /// <returns>12345</returns>
    IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads);
}
