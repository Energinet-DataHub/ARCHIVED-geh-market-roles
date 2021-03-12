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

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Energinet.DataHub.MarketData.Application.Outbox
{
    public class ForwardMessageService
    {
        private readonly IForwardMessageRepository _forwardMessageRepository;

        public ForwardMessageService(
            IForwardMessageRepository forwardMessageRepository)
        {
            _forwardMessageRepository = forwardMessageRepository;
        }

        public async Task ProcessMessagesAsync(ILogger logger)
        {
            var message = await _forwardMessageRepository.GetUnprocessedForwardMessageAsync().ConfigureAwait(false);

            while (message != null)
            {
                // TODO: Create logic for dispatching to CosmosDB and remove the log (+ Newtonsoft)
                // For the sake of demo it logged to application insight for now
                logger.LogInformation(new JObject
                  {
                      { "Type", message.Type },
                      { "OccurredOn", message.OccurredOn.ToString() },
                      { "Data", JObject.Parse(message.Data!) },
                  }.ToString());

                await _forwardMessageRepository.MarkForwardedMessageAsProcessedAsync(message.Id).ConfigureAwait(false);

                message = await _forwardMessageRepository.GetUnprocessedForwardMessageAsync().ConfigureAwait(false);
            }
        }
    }
}
