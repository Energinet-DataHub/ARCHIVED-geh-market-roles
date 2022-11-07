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

using Processing.Domain.Customers;
using Processing.Domain.EnergySuppliers;

namespace Processing.Domain.Contracts
{
    public class ContractDetails
    {
        private ContractDetails(Customer customer, EnergySupplierId energySupplierId)
        {
            Customer = customer;
            EnergySupplierId = energySupplierId;
        }

        public Customer Customer { get; }

        public EnergySupplierId EnergySupplierId { get; }

        internal static ContractDetails Create(Customer customer, EnergySupplierId energySupplierId)
        {
            return new ContractDetails(customer, energySupplierId);
        }
    }
}
