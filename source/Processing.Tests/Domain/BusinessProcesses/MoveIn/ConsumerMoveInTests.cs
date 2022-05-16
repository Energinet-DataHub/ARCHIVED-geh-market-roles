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
using Processing.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Processing.Domain.SeedWork;
using Xunit;

namespace Processing.Tests.Domain.BusinessProcesses.MoveIn;

public class ConsumerMoveInTests
{
    private readonly AccountingPoint _accountingPoint;
    private readonly Consumer _consumer;
    private readonly EnergySupplier _energySupplier;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ConsumerMoveIn _consumerMoveIn;
    private readonly Transaction _transaction;

    public ConsumerMoveInTests()
    {
        _consumerMoveIn = new ConsumerMoveIn();
        _systemDateTimeProvider = new SystemDateTimeProviderStub();
        _accountingPoint = AccountingPoint.CreateProduction(GsrnNumber.Create(SampleData.GsrnNumber), true);
        _consumer = new Consumer(ConsumerId.New(), CprNumber.Create(SampleData.ConsumerSocialSecurityNumber), ConsumerName.Create(SampleData.ConsumerName));
        _energySupplier = new EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
        _transaction = Transaction.Create(SampleData.Transaction);
    }

    [Fact]
    public void Throw_if_any_business_rules_are_broken()
    {
        _consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, SystemClock.Instance.GetCurrentInstant(), _transaction);

        Assert.Throws<BusinessProcessException>(() => _consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, SystemClock.Instance.GetCurrentInstant(), _transaction));
    }

    [Fact]
    public void Consumer_move_in_is_accepted()
    {
        _consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, SystemClock.Instance.GetCurrentInstant(), _transaction);

        Assert.Contains(_accountingPoint.DomainEvents, e => e is ConsumerMoveInAccepted);
    }

    [Fact]
    public void Cannot_move_in_on_a_date_where_a_move_in_is_already_registered()
    {
        var moveInDate = _systemDateTimeProvider.Now();

        _consumerMoveIn.StartProcess(_accountingPoint, _consumer, _energySupplier, moveInDate, _transaction);

        var result = _consumerMoveIn.CheckRules(_accountingPoint, moveInDate);
        Assert.Contains(result.Errors, error => error is MoveInRegisteredOnSameDateIsNotAllowedRuleError);
    }

    [Fact]
    public void Cannot_register_a_move_in_on_a_date_where_a_move_in_is_already_effectuated()
    {
        var moveInDate = _systemDateTimeProvider.Now();
        _accountingPoint.AcceptConsumerMoveIn(_consumer.ConsumerId, _energySupplier.EnergySupplierId, moveInDate, _transaction);
        _accountingPoint.EffectuateConsumerMoveIn(_transaction, _systemDateTimeProvider);

        var result = _accountingPoint.ConsumerMoveInAcceptable(moveInDate);

        Assert.Contains(result.Errors, error => error is MoveInRegisteredOnSameDateIsNotAllowedRuleError);
    }
}
