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

using NodaTime;
using Processing.Domain.Customers;
using Processing.Domain.SeedWork;

namespace Processing.Domain.MeteringPoints
{
    public class ConsumerRegistration : Entity
    {
        public ConsumerRegistration(Customer customer, BusinessProcessId businessProcessId)
        {
            Id = ConsumerRegistrationId.New();
            Customer = customer;
            BusinessProcessId = businessProcessId;
        }

        private ConsumerRegistration(BusinessProcessId businessProcessId)
        {
            Id = ConsumerRegistrationId.New();
            BusinessProcessId = businessProcessId;
        }

        public ConsumerRegistrationId Id { get; }

        public Customer? Customer { get; private set; }

        public Customer? SecondCustomer { get; private set; }

        public Instant? MoveInDate { get; private set; }

        public BusinessProcessId BusinessProcessId { get; }

        public void SetMoveInDate(Instant effectiveDate)
        {
            MoveInDate = effectiveDate;
        }

        public void UpdateCustomer(Customer customer)
        {
            Customer = customer;
        }

        public void UpdateSecondCustomer(Customer customer)
        {
            SecondCustomer = customer;
        }
    }
}
