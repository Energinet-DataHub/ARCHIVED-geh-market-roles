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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using FluentAssertions;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Queues.ValidationReportDispatcher;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using Microsoft.Extensions.Logging;
using Moq;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesCommandHandlerTests
    {
        [Theory]
        [InlineAutoDomainData]
        public async Task AcceptAsync_WhenCalled_ShouldVerifyThatLoggerIsCalledWithSuccesfulMessage(
            [Frozen] Mock<ILogger<ChangeOfChargesCommandHandler>> logger,
            ChangeOfChargesMessage message,
            ChangeOfChargesCommandHandlerTestable sut)
        {
            await sut.CallAcceptAsync(message).ConfigureAwait(false);

            logger.VerifyLoggerWasCalled($"{nameof(ChangeOfChargesMessage)} have parsed validation", LogLevel.Information);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task RejectAsync_WhenCalled_ShouldSendEventToReportQueue([Frozen] Mock<IValidationReportQueueDispatcher> validationReportQueueDispatcher, ChangeOfChargesMessage message, [NotNull]ChangeOfChargesCommandHandlerTestable sut)
        {
            await sut.CallRejectAsync(message);

            validationReportQueueDispatcher.Verify(mock => mock.DispatchAsync(Moq.It.IsAny<IHubMessage>()), Times.Once);
        }

        [Theory]
        [InlineAutoDomainData(-10, false)]
        [InlineAutoDomainData(50, true)]
        public async Task ValidateAsync_WhenCalled_ShouldReturnExpectedResultDependingOnParameter(int daysFromNow, bool validationParsed, [NotNull]ChangeOfChargesCommandHandlerTestable sut)
        {
            ChangeOfChargesMessage changeOfChargesMessage = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord { ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(daysFromNow)) },
            };

            var validationResult = await sut.CallValidateAsync(changeOfChargesMessage).ConfigureAwait(false);

            validationResult.Should().Be(validationParsed);
        }
    }
}
