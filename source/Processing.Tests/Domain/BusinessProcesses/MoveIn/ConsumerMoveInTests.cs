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
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Xunit;

namespace Processing.Tests.Domain.BusinessProcesses.MoveIn;

public class ConsumerMoveInTests
{
    private readonly AccountingPoint _accountingPoint;
    private readonly Consumer _consumer;
    private readonly EnergySupplier _energySupplier;

    public ConsumerMoveInTests()
    {
        _accountingPoint = AccountingPoint.CreateProduction(GsrnNumber.Create(SampleData.GsrnNumber), true);
        _consumer = new Consumer(ConsumerId.New(), CprNumber.Create(SampleData.ConsumerSocialSecurityNumber), ConsumerName.Create(SampleData.ConsumerName));
        _energySupplier = new EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
    }

    [Fact]
    public void Throw_if_any_business_rules_are_broken()
    {
        var consumerMoveIn = new ConsumerMoveIn();
        consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, SystemClock.Instance.GetCurrentInstant(), Transaction.Create(SampleData.Transaction));

        Assert.Throws<BusinessProcessException>(() => consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, SystemClock.Instance.GetCurrentInstant(), Transaction.Create(SampleData.Transaction)));
    }

    [Fact]
    public void Consumer_move_in_is_accepted()
    {
        var consumerMoveIn = new ConsumerMoveIn();
        consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, SystemClock.Instance.GetCurrentInstant(), Transaction.Create(SampleData.Transaction));

        Assert.Contains(_accountingPoint.DomainEvents, e => e is ConsumerMoveInAccepted);
    }
}
