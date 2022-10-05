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

using System.Collections.ObjectModel;
using System.Linq;
using NodaTime;
using Processing.Domain.BusinessProcesses.MoveIn.Errors;
using Processing.Domain.Customers;
using Processing.Domain.SeedWork;

namespace Processing.Domain.MeteringPoints.Rules.MoveIn
{
    public class CustomerMustBeDifferentFromCurrentCustomerRule : IBusinessRule
    {
        private readonly Customer _customer;
        private readonly ReadOnlyCollection<ConsumerRegistration> _consumerRegistrations;
        private readonly Instant _today;

        public CustomerMustBeDifferentFromCurrentCustomerRule(Customer customer, ReadOnlyCollection<ConsumerRegistration> consumerRegistrations, Instant today)
        {
            _customer = customer;
            _consumerRegistrations = consumerRegistrations;
            _today = today;
            CheckCustomer();
        }

        public bool IsBroken { get; private set; }

        public ValidationError ValidationError { get; } = new CustomerMustBeDifferentFromCurrentCustomer();

        private void CheckCustomer()
        {
            var currentCustomer = _consumerRegistrations
                .OrderByDescending(c => c.MoveInDate)
                .Last(c => c.MoveInDate < _today)
                .Customer;

            if (currentCustomer == null)
            {
                IsBroken = false;
            }
            else
            {
                IsBroken = currentCustomer.Equals(_customer);
            }
        }
    }
}
