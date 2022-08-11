using System.Threading.Tasks;
using System.Xml;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.Common;

/// <summary>
/// bla
/// </summary>
public interface IHeaderWriter
{
    /// <summary>
    /// bla bla
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="documentDetails"></param>
    /// <returns>bla</returns>
    public Task WriteAsync(MessageHeader messageHeader, DocumentDetails documentDetails);
}
