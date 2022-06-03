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
using Azure.Messaging.ServiceBus;

namespace Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus
{
    public sealed class ServiceBusSenderFactory : IServiceBusSenderFactory
    {
        private readonly Dictionary<string, IServiceBusSenderAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);
        private readonly ServiceBusClient _serviceBusClient;

        public ServiceBusSenderFactory(ServiceBusClient serviceBusClient)
        {
            _serviceBusClient = serviceBusClient;
        }

        public IServiceBusSenderAdapter GetSender(string topicName)
        {
            _adapters.TryGetValue(topicName, out var adapter);
            if (adapter is null)
            {
                adapter = new ServiceBusSenderAdapter(_serviceBusClient, topicName);
                _adapters.Add(topicName, adapter);
            }

            return adapter;
        }

        #pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public async ValueTask DisposeAsync()
        {
            foreach (var serviceBusSenderAdapter in _adapters)
            {
                await serviceBusSenderAdapter.Value.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            foreach (var serviceBusSenderAdapter in _adapters)
            {
                serviceBusSenderAdapter.Value.Dispose();
            }
        }
    }
}
