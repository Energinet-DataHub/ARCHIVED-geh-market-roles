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
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Processing.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Xunit;

namespace Processing.Tests.Domain.MeteringPoints.MoveIn
{
    public class AcceptTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;
        private readonly AccountingPoint _accountingPoint;
        private ConsumerId _consumerId;
        private EnergySupplierId _energySupplierId;
        private Transaction _transaction;

        public AcceptTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
            _consumerId = ConsumerId.New();
            _energySupplierId = EnergySupplierId.New();
            _transaction = Transaction.Create(Guid.NewGuid().ToString());
            _accountingPoint = Create();
        }

        private static AccountingPoint Create()
        {
            var gsrnNumber = GsrnNumber.Create(SampleData.GsrnNumber);
            return new AccountingPoint(gsrnNumber, MeteringPointType.Consumption);
        }
    }
}
