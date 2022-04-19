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
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2B.Transactions.IntegrationTests.Infrastructure.OutgoingMessages
{
    public class MessageRequestTests
    {
        [Fact]
        public async Task Message_is_forwarded_on_request()
        {
            List<Guid> messageIdsToForward = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid() };
            var messageForwarder = new MessageForwarderSpy();

            await messageForwarder.ForwardAsync(messageIdsToForward).ConfigureAwait(false);

            Assert.NotNull(messageForwarder.ForwardedMessages);
        }
    }

#pragma warning disable
    public class MessageForwarderSpy
    {

        public Task ForwardAsync(List<Guid> messageIdsToForward)
        {
            foreach (var messageId in messageIdsToForward)
            {
                ForwardedMessages.Add(messageId);
            }
            return Task.CompletedTask;
        }

        public List<Guid> ForwardedMessages { get; } = new ();
    }
}
