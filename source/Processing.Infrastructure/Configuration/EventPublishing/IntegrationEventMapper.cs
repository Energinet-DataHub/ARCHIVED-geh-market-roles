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
using System.Linq;

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class IntegrationEventMapper
    {
        private readonly HashSet<EventMetadata> _values = new();

        public void Add(string eventName, Type eventType, int version, string topicName)
        {
            _values.Add(new EventMetadata(eventName, eventType, version, topicName));
        }

        public EventMetadata GetByName(string eventName)
        {
            return _values
                .FirstOrDefault(metadata => metadata.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException(
                $"No event metadata is registered for event {eventName}");
        }

        public EventMetadata GetByType(Type eventType)
        {
            if (eventType == null) throw new ArgumentNullException(nameof(eventType));
            return _values
                       .FirstOrDefault(metadata => metadata.EventType == eventType) ??
                   throw new InvalidOperationException(
                       $"No event metadata is registered for type {eventType.FullName}");
        }
    }

    public record EventMetadata(string EventName, Type EventType, int Version, string TopicName);
}
