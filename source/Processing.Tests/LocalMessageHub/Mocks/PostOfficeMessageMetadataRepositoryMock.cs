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
using System.Linq;
using System.Threading.Tasks;
using Processing.Infrastructure.LocalMessageHub;

namespace Processing.Tests.LocalMessageHub.Mocks
{
    public class MessageHubMessageRepositoryMock : IMessageHubMessageRepository
    {
        internal List<MessageHubMessage> MessageHubMessages { get; } = new();

        public Task<MessageHubMessage> GetMessageAsync(Guid messageId)
        {
            return Task.FromResult(MessageHubMessages.First(x => x.Id == messageId));
        }

        public Task<MessageHubMessage[]> GetMessagesAsync(Guid[] messageIds)
        {
            return Task.FromResult(MessageHubMessages.ToArray());
        }

        public void AddMessageMetadata(MessageHubMessage messageHubMessage)
        {
            MessageHubMessages.Add(messageHubMessage);
        }

        public MessageHubMessage GetMessageByCorrelation(string correlation)
        {
            return MessageHubMessages.Single(x => x.Correlation == correlation);
        }
    }
}
