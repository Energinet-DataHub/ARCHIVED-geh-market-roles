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

namespace Processing.Infrastructure.Configuration.EventPublishing.AzureServiceBus
{
    /// <summary>
    /// Azure Service Bus Client sender adapter
    /// </summary>
    public interface IServiceBusSenderAdapter : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Topic name
        /// </summary>
        string TopicName { get; }

        /// <summary>
        /// Send integration event to topic
        /// </summary>
        /// <param name="message"></param>
        Task SendAsync(ServiceBusMessage message);
    }
}
