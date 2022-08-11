using System.Threading.Tasks;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.Common;

public class JsonHeaderWriter : IHeaderWriter
{
    public Task WriteAsync(MessageHeader messageHeader, DocumentDetails documentDetails)
    {
        return Task.CompletedTask;
    }
}
