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
using Processing.Domain.Customers;
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
    private readonly EnergySupplier _energySupplier;
    private readonly CustomerMoveIn _customerMoveInProcess;
    private readonly BusinessProcessId _processId;
    private readonly Customer _customer;

    public ConsumerMoveInTests()
    {
        CurrentSystemTimeIsSummertime();
        _customerMoveInProcess = new CustomerMoveIn(EffectiveDatePolicyFactory.CreateEffectiveDatePolicy());
        _accountingPoint = AccountingPoint.CreateProduction(AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber), true);
        _energySupplier = new EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
        _processId = BusinessProcessId.New();
        _customer = Customer.Create(CustomerNumber.Create(SampleData.ConsumerSocialSecurityNumber), SampleData.ConsumerName);
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
        var moveProcess = new CustomerMoveIn(policy);

        var moveInDate = AsOf(SystemDateTimeProvider.Now().Plus(Duration.FromDays(10)));
        var result = moveProcess.CanStartProcess(_accountingPoint, moveInDate, SystemDateTimeProvider.Now(), _customer);

        AssertError<EffectiveDateIsNotWithinAllowedTimePeriod>(result, "EffectiveDateIsNotWithinAllowedTimePeriod");
    }

    [Fact]
    public void Customer_must_be_different_from_current_customer()
    {
        GivenACustomerIsRegistered();

        var result = _customerMoveInProcess.CanStartProcess(_accountingPoint, AsOfToday(), SystemDateTimeProvider.Now(), _customer);

        AssertError<CustomerMustBeDifferentFromCurrentCustomer>(result, "CustomerMustBeDifferentFromCurrentCustomer");
    }

    private static EffectiveDate AsOf(Instant date)
    {
        return EffectiveDateFactory.WithTimeOfDay(date.ToDateTimeUtc(), 22, 0, 0);
    }

    private void GivenACustomerIsRegistered()
    {
        StartProcess(AsOfYesterday());
    }

    private BusinessRulesValidationResult CanStartProcess(EffectiveDate moveInDate)
    {
        return _customerMoveInProcess.CanStartProcess(_accountingPoint, moveInDate, SystemDateTimeProvider.Now(), _customer);
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
        _customerMoveInProcess.StartProcess(
            _accountingPoint,
            _energySupplier,
            moveInDate,
            SystemDateTimeProvider.Now(),
            _processId,
            _customer);
    }
}
