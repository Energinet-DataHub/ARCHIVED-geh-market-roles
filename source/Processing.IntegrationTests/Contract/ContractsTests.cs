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
using JetBrains.Annotations;
using NodaTime;
using Processing.Domain.Customers;
using Processing.IntegrationTests.Application;
using Processing.IntegrationTests.Fixtures;
using Xunit;

namespace Processing.IntegrationTests.Contract
{
#pragma warning disable
    public class ContractsTests : TestBase
    {
        private readonly ContractRepositorySpy _contractRepository;

        public ContractsTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _contractRepository = new ContractRepositorySpy();
        }

        [Fact]
        public void Start_of_supply_is_set()
        {
            CreateContract();

            var contract = _contractRepository.GetByBusinessProcessId(SampleData.BusinessProcessId);
            var startOfSupplyDate = SystemDateTimeProvider.Now();
            contract.SetStartOfSupply(startOfSupplyDate);

            Assert.Equal(startOfSupplyDate, contract.StartOfSupplyDate);
        }

        private void CreateContract()
        {
            _contractRepository.Add(Contract.Create(SampleData.BusinessProcessId,
                Customer.Create(CustomerNumber.Create("2605919995"), "Customer Name 1"), "EnergySupplierId 1"));
        }

    }

    public class ContractRepositorySpy
    {
        private readonly List<Contract> _contracts = new List<Contract>();

        public void Add(Contract contract)
        {
            _contracts.Add(contract);
        }

        public Contract GetByBusinessProcessId(Guid businessProcessId)
        {
            return _contracts.FirstOrDefault(x => x.BusinessProcessId == businessProcessId);
        }
    }

    public class Contract
    {
        private Contract(Guid businessProcessId, Customer customer, string energySupplierId)
        {
            Id = Guid.NewGuid();
            BusinessProcessId = businessProcessId;
            Customer = customer;
            EnergySupplierId = energySupplierId;
        }
        public Guid Id { get; set; }

        public Guid BusinessProcessId { get; set; }

        public Customer Customer { get; set; }

        public Customer? SecondCustomer { get; set; }

        public bool? ProtectedName { get; set; }

        public string EnergySupplierId { get; set; }

        public Instant? StartOfSupplyDate { get; set; }

        public Instant? EndOfSupplyDate { get; set; }

        public Address? TechnicalAddress { get; set; }

        public Address? LegalAddress { get; set; }

        public static Contract Create(Guid businessProcessId, Customer customer, string energySupplierId)
        {
            return new Contract(businessProcessId, customer, energySupplierId);
        }

        public void SetStartOfSupply(Instant startOfSupplyDate)
        {
            StartOfSupplyDate = startOfSupplyDate;
        }


    }

    public record Address();

}
