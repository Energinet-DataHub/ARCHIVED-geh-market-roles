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
using Processing.Domain.BusinessProcesses.MoveIn.Errors;
using Processing.Domain.Common;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Processing.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Processing.Domain.SeedWork;
using Xunit;

namespace Processing.Tests.Domain.BusinessProcesses.MoveIn;

public class ConsumerMoveInTests : TestBase
{
    private readonly AccountingPoint _accountingPoint;
    private readonly Consumer _consumer;
    private readonly Processing.Domain.EnergySuppliers.EnergySupplier _energySupplier;
    private readonly ConsumerMoveIn _consumerMoveInProcess;
    private readonly BusinessProcessId _processId;

    public ConsumerMoveInTests()
    {
        CurrentSystemTimeIsSummertime();
        _consumerMoveInProcess = new ConsumerMoveIn(EffectiveDatePolicyFactory.CreateEffectiveDatePolicy());
        _accountingPoint = AccountingPoint.CreateProduction(AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber), true);
        _consumer = new Consumer(ConsumerId.New(), CprNumber.Create(SampleData.ConsumerSocialSecurityNumber), ConsumerName.Create(SampleData.ConsumerName));
        _energySupplier = new Processing.Domain.EnergySuppliers.EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
        _processId = BusinessProcessId.New();
    }

    [Fact]
    public void Move_in_is_effectuated_if_effective_date_is_in_the_past()
    {
        StartProcess(AsOfYesterday());

        Assert.Contains(_accountingPoint.DomainEvents, @event => @event is ConsumerMovedIn);
    }

    [Fact]
    public void Throw_if_any_business_rules_are_broken()
    {
        StartProcess(AsOfToday());

        Assert.Throws<BusinessProcessException>(() => StartProcess(AsOfToday()));
    }

    [Fact]
    public void Consumer_move_in_is_accepted()
    {
        StartProcess(AsOfToday());

        Assert.Contains(_accountingPoint.DomainEvents, e => e is ConsumerMoveInAccepted);
    }

    [Fact]
    public void Cannot_move_in_on_a_date_where_a_move_in_is_already_registered()
    {
        var moveInDate = AsOfToday();

        StartProcess(moveInDate);

        var result = CanStartProcess(moveInDate);

        AssertError<MoveInRegisteredOnSameDateIsNotAllowedRuleError>(result);
    }

    [Fact]
    public void Cannot_register_a_move_in_on_a_date_where_a_move_in_is_already_effectuated()
    {
        var moveInDate = AsOfToday();
        StartProcess(moveInDate);
        _accountingPoint.EffectuateConsumerMoveIn(_processId, SystemDateTimeProvider.Now());

        var result = CanStartProcess(moveInDate);

        AssertError<MoveInRegisteredOnSameDateIsNotAllowedRuleError>(result);
    }

    [Fact]
    public void Effective_date_must_be_within_allowed_time_range()
    {
        var maxNumberOfDaysAheadOfcurrentDate = 5;
        var policy = EffectiveDatePolicyFactory.CreateEffectiveDatePolicy(maxNumberOfDaysAheadOfcurrentDate);
        var moveProcess = new ConsumerMoveIn(policy);

        var moveInDate = AsOf(SystemDateTimeProvider.Now().Plus(Duration.FromDays(10)));
        var result = moveProcess.CanStartProcess(_accountingPoint, moveInDate, SystemDateTimeProvider.Now());

        AssertError<EffectiveDateIsNotWithinAllowedTimePeriod>(result, "EffectiveDateIsNotWithinAllowedTimePeriod");
    }

    private static EffectiveDate AsOf(Instant date)
    {
        return EffectiveDateFactory.WithTimeOfDay(date.ToDateTimeUtc(), 22, 0, 0);
    }

    private BusinessRulesValidationResult CanStartProcess(EffectiveDate moveInDate)
    {
        return _consumerMoveInProcess.CanStartProcess(_accountingPoint, moveInDate, SystemDateTimeProvider.Now());
    }

    private EffectiveDate AsOfToday()
    {
        return EffectiveDateFactory.WithTimeOfDay(SystemDateTimeProvider.Now().ToDateTimeUtc(), 22, 0, 0);
    }

    private EffectiveDate AsOfYesterday()
    {
        return EffectiveDateFactory.WithTimeOfDay(SystemDateTimeProvider.Now().Minus(Duration.FromDays(1)).ToDateTimeUtc(), 22, 0, 0);
    }

    private void StartProcess(EffectiveDate moveInDate)
    {
        _consumerMoveInProcess.StartProcess(_accountingPoint, _consumer, _energySupplier, moveInDate, SystemDateTimeProvider.Now(), _processId);
    }
}
