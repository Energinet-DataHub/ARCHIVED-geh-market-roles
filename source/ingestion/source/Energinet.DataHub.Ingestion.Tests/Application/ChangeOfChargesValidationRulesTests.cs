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

using System.Linq;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using FluentAssertions;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using GreenEnergyHub.TestHelpers.Traits;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesValidationRulesTests
    {
        [Theory]
        [InlineData(32)]
        [InlineData(365)]
        [InlineData(1095)]
        public void Validate_WhenCalledWithValidDate_ShouldReturnTrue(int changeOfChargesStartDay)
        {
            ChangeOfChargesMessage messageWithLowStartDate = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord { ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(changeOfChargesStartDay)) },
            };

            var hubRequestValidationResult = new HubRequestValidationResult("someMrId");

            ChangeOfChargesValidationRules.Validate(messageWithLowStartDate, hubRequestValidationResult);

            hubRequestValidationResult.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(15)]
        [InlineData(31)]
        public void Validate_WhenCalledWithTooEarlyDate_ShouldReturnFalse(int changeOfChargesStartDay)
        {
            ChangeOfChargesMessage messageWithLowStartDate = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord { ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(changeOfChargesStartDay)) },
            };

            var hubRequestValidationResult = new HubRequestValidationResult("someMrId");

            ChangeOfChargesValidationRules.Validate(messageWithLowStartDate, hubRequestValidationResult);

            var error = hubRequestValidationResult.Errors.Single();
            error.Code.Should().Be("VR209");
        }

        [Theory]
        [InlineData(1096)]
        public void Validate_WhenCalledWithTooLateDate_ShouldReturnFalse(int changeOfChargesStartDay)
        {
            ChangeOfChargesMessage messageWithLowStartDate = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord { ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(changeOfChargesStartDay)) },
            };

            var hubRequestValidationResult = new HubRequestValidationResult("someMrId");

            ChangeOfChargesValidationRules.Validate(messageWithLowStartDate, hubRequestValidationResult);

            var error = hubRequestValidationResult.Errors.Single();
            error.Code.Should().Be("VR209");
        }
    }
}
