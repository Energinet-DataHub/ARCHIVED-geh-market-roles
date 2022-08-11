using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Messaging.Domain.OutgoingMessages;

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
    /// Supah bla
    /// </summary>
    /// <param name="marketActivityPayloads"></param>
    /// <returns>Supah dupah bla</returns>
    Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads);

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
