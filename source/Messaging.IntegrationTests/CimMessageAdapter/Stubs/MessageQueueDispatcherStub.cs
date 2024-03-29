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
using Messaging.Application.IncomingMessages;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.Queues;

namespace Messaging.IntegrationTests.CimMessageAdapter.Stubs
{
    public class MessageQueueDispatcherStub<TQueue> : IMessageQueueDispatcher<TQueue>
    where TQueue : Queue
    {
        private readonly List<IMarketTransaction> _uncommittedItems = new();
        private readonly List<IMarketTransaction> _committedItems = new();

        public IReadOnlyCollection<IMarketTransaction> CommittedItems => _committedItems.AsReadOnly();

        public Task AddAsync(IMarketTransaction message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _committedItems.Clear();
            _uncommittedItems.Add(message);
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            _committedItems.Clear();
            _committedItems.AddRange(_uncommittedItems);
            _uncommittedItems.Clear();
            return Task.CompletedTask;
        }
    }
}
