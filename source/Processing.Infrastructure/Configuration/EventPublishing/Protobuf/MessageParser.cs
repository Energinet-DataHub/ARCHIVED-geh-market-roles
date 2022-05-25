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
using System.Reflection;
using Contracts.IntegrationEvents;
using Google.Protobuf;

namespace Processing.Infrastructure.Configuration.EventPublishing.Protobuf
{
    internal static class MessageParser
    {
        internal static IMessage GetFrom(string integrationEventTypeName, string payload)
        {
            var eventType = typeof(ConsumerMovedIn).Assembly.GetType(integrationEventTypeName);
            if (eventType is null)
            {
                throw new InvalidOperationException($"Could not get type '{integrationEventTypeName}'");
            }

            var descriptor = (Google.Protobuf.Reflection.MessageDescriptor)eventType
                .GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null, null)!;

            if (descriptor is null)
            {
                throw new InvalidOperationException($"The property 'Descriptor' does not exist on type {eventType.Name}");
            }

            return descriptor.Parser.ParseJson(payload);
        }
    }
}
