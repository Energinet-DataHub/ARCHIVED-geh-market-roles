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
using System.Threading.Tasks;
using Processing.Application.Common.Commands;
using Processing.Application.Common.Processing;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Processing;
using Processing.Domain.MeteringPoints;
using Xunit;
using Xunit.Categories;

namespace Processing.IntegrationTests.Application.MoveIn.Processing
{
    [IntegrationTest]
    public class MoveInProcessManagerTests : TestHost
    {
        private readonly MoveInProcessManagerRouter _router;

        public MoveInProcessManagerTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _router = new MoveInProcessManagerRouter(GetService<IProcessManagerRepository>(), GetService<ICommandScheduler>());
        }

        [Fact]
        public async Task ConsumerMoveInAccepted_WhenStateIsNotStarted_EffectuateCommandIsEnqueued()
        {
            var businessProcessId = await SetupScenario().ConfigureAwait(false);

            var command = await GetEnqueuedCommandAsync<EffectuateConsumerMoveIn>(businessProcessId).ConfigureAwait(false);

            Assert.NotNull(command);
            Assert.Equal(businessProcessId.Value.ToString(), command?.ProcessId);
        }

        [Fact]
        public async Task ConsumerMoveIn_WhenStateIsAwaitingEffectuation_ProcessIsCompleted()
        {
            var businessProcessId = await SetupScenario().ConfigureAwait(false);

            var effectuateConsumerMoveInCommand = await GetEnqueuedCommandAsync<EffectuateConsumerMoveIn>(businessProcessId).ConfigureAwait(false);
            await InvokeCommandAsync(effectuateConsumerMoveInCommand!).ConfigureAwait(false);

            var processManager = await ProcessManagerRepository.GetAsync<MoveInProcessManager>(businessProcessId).ConfigureAwait(false);
            Assert.True(processManager?.IsCompleted());
        }

        private async Task<BusinessProcessId> SetupScenario()
        {
            _ = CreateAccountingPoint();
            _ = CreateEnergySupplier(Guid.NewGuid(), SampleData.GlnNumber);
            _ = CreateConsumer();
            SaveChanges();

            var result = await SendRequestAsync(new MoveInRequest(
                new Consumer(SampleData.ConsumerName, SampleData.ConsumerSSN, "CPR"),
                SampleData.GlnNumber,
                SampleData.GsrnNumber,
                SampleData.MoveInDate)).ConfigureAwait(false);

            if (result.ProcessId is null)
            {
                throw new InvalidOperationException("Setup scenario failed.");
            }

            return BusinessProcessId.Create(result.ProcessId);
        }
    }
}
