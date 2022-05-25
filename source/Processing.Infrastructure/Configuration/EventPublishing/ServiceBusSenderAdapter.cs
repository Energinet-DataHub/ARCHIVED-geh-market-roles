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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class ServiceBusSenderAdapter : IServiceBusSenderAdapter, IAsyncDisposable
    {
        private readonly ServiceBusSender _serviceBusSender;

        public ServiceBusSenderAdapter(ServiceBusClient serviceBusClient, string topicName)
        {
            if (serviceBusClient == null) throw new ArgumentNullException(nameof(serviceBusClient));
            TopicName = topicName;
            _serviceBusSender = serviceBusClient.CreateSender(topicName);
        }

        public string TopicName { get; }

        public Task SendAsync(ServiceBusMessage message)
        {
            return _serviceBusSender.SendMessageAsync(message);
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceBusSender.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
