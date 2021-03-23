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
using Energinet.DataHub.MarketData.Domain.Customers;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public class Consumer : Entity
    {
        public Consumer(CustomerId customerId, Instant moveInOn)
        {
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            MoveInOn = moveInOn;
        }

        private Consumer(int id, CustomerId customerId, Instant moveInOn, Instant moveOutOn)
        {
            Id = id;
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            MoveInOn = moveInOn;
            MoveOutOn = moveOutOn;
        }

        public CustomerId CustomerId { get; }

        public Instant MoveInOn { get; }

        public Instant MoveOutOn { get; private set; }

        public static Consumer CreateFrom(ConsumerSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new Consumer(
                snapshot.Id,
                new CustomerId(snapshot.CustomerId),
                snapshot.MoveInOn,
                snapshot.MoveOutOn);
        }

        public ConsumerSnapshot GetSnapshot()
        {
            return new ConsumerSnapshot(Id, CustomerId.Value, MoveInOn, MoveOutOn);
        }

        public void MoveOut(Instant moveOn)
        {
            MoveOutOn = moveOn;
        }
    }
}
