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
using Processing.Domain.Consumers.Events;
using Processing.Domain.SeedWork;

namespace Processing.Domain.Consumers
{
    public class Consumer : AggregateRootBase
    {
        public Consumer(ConsumerId consumerId, CprNumber cprNumber, ConsumerName name)
            : this(consumerId, name)
        {
            ConsumerId = consumerId ?? throw new ArgumentNullException(nameof(consumerId));
            CprNumber = cprNumber ?? throw new ArgumentNullException(nameof(cprNumber));
            AddDomainEvent(new ConsumerCreated(ConsumerId.Value, CprNumber.Value, null, Name.FullName));
        }

        public Consumer(ConsumerId consumerId, CvrNumber cvrNumber, ConsumerName name)
            : this(consumerId, name)
        {
            ConsumerId = consumerId ?? throw new ArgumentNullException(nameof(consumerId));
            CvrNumber = cvrNumber ?? throw new ArgumentNullException(nameof(cvrNumber));
            AddDomainEvent(new ConsumerCreated(ConsumerId.Value, null, CvrNumber.Value, Name.FullName));
        }

        private Consumer(ConsumerId consumerId, ConsumerName name)
        {
            ConsumerId = consumerId ?? throw new ArgumentNullException(nameof(consumerId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public ConsumerName Name { get; }

        public ConsumerId ConsumerId { get; }

        public CprNumber? CprNumber { get; }

        public CvrNumber? CvrNumber { get; }
    }
}
