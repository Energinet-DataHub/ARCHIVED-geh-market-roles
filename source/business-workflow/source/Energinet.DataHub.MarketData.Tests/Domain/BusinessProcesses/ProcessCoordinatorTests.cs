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
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Events;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Exceptions;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.ProcessCoordinators;
using GreenEnergyHub.TestHelpers.Traits;
using Moq;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.BusinessProcesses
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ProcessCoordinatorTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;

        public ProcessCoordinatorTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Register_WhenNoBlockingProcessIsRegistered_IsSuccess()
        {
            var processCoordinator = Create();

            var processId = CreateProcessId();
            var effectiveDate = _systemDateTimeProvider.Now();

            processCoordinator.Register(new ChangeOfSupplierProcess(processId, effectiveDate));

            var @event = processCoordinator.DomainEvents!.First() as ProcessRegistered;

            Assert.Equal(processId, @event!.ProcessId);
        }

        [Fact]
        public void Register_WhenProcessIdIsAlreadyRegistered_ShouldThrow()
        {
            var processCoordinator = Create();
            var processId = CreateProcessId();
            var effectiveDate = _systemDateTimeProvider.Now();
            processCoordinator.Register(new ChangeOfSupplierProcess(processId, effectiveDate));

            Assert.Throws<DuplicateProcessRegistrationException>(() =>
                processCoordinator.Register(new ChangeOfSupplierProcess(processId, effectiveDate)));
        }

        [Fact]
        public void Register_WhenActiveBlockingProcessesWithSameEffectiveDate_ShouldThrow()
        {
            var processCoordinator = Create();

            var effectiveDate = _systemDateTimeProvider.Now();
            processCoordinator.Register(new MoveInProcess(CreateProcessId(), effectiveDate));

            Assert.Throws<BlockingProcessRegisteredException>(() =>
                processCoordinator.Register(new ChangeOfSupplierProcess(CreateProcessId(), effectiveDate)));
        }

        [Fact]
        public void Register_WhenInactiveBlockingProcessesWithSameEffectiveDate_IsSuccess()
        {
            var processCoordinator = Create();

            var effectiveDate = _systemDateTimeProvider.Now();
            var processId = CreateProcessId();
            processCoordinator.Register(new MoveInProcess(processId, effectiveDate));
            processCoordinator.Cancel(processId);

            processCoordinator.Register(new ChangeOfSupplierProcess(CreateProcessId(), effectiveDate));
            var @event = processCoordinator.DomainEvents!.First() as ProcessRegistered;

            Assert.Equal(processId, @event!.ProcessId);
        }

        [Fact]
        public void Cancel_HasSuspendedProcesses_ShouldReactivateSuspended()
        {
            var processCoordinator = Create();

            var changeOfSupplierProcessId = CreateProcessId();
            processCoordinator.Register(new ChangeOfSupplierProcess(changeOfSupplierProcessId, _systemDateTimeProvider.Now().Plus(Duration.FromDays(10))));

            var moveInProcessId = CreateProcessId();
            processCoordinator.Register(new MoveInProcess(moveInProcessId, _systemDateTimeProvider.Now()));

            processCoordinator.Cancel(moveInProcessId);

            var process = processCoordinator.GetProcessOrThrow(changeOfSupplierProcessId);
            var @event = process.DomainEvents!.First(e => e is ProcessReactivated) as ProcessReactivated;

            Assert.Equal(ProcessState.Registered, process.State);
            Assert.Equal(changeOfSupplierProcessId, @event!.ProcessId);
        }

        [Fact]
        public void RegisterMoveIn_WhenFutureSuspendableProcessesesRegistered_ShouldSuspendFutureProcesses()
        {
            var processCoordinator = Create();

            var changeOfSupplierProcessId = CreateProcessId();
            var changeOfSupplierEffectiveDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(5));
            processCoordinator.Register(new ChangeOfSupplierProcess(changeOfSupplierProcessId, changeOfSupplierEffectiveDate));

            var moveInEffectiveDate = _systemDateTimeProvider.Now();
            var moveInProcessId = CreateProcessId();
            processCoordinator.Register(new MoveInProcess(moveInProcessId, moveInEffectiveDate));

            var changeOfSupplierProcess = processCoordinator.GetProcessOrThrow(changeOfSupplierProcessId);
            Assert.Equal(ProcessState.Suspended, changeOfSupplierProcess.State);
            Assert.Equal(moveInProcessId, changeOfSupplierProcess.SuspendedByProcessId);
        }

        [Fact]
        public async Task ProcessCoordinator_can_be_saved_and_retrieved_via_repository()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var processCoordinator = Create();

            var changeOfSupplierProcessId = CreateProcessId();
            var changeOfSupplierEffectiveDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(5));
            processCoordinator.Register(new ChangeOfSupplierProcess(changeOfSupplierProcessId, changeOfSupplierEffectiveDate));

            var moveInEffectiveDate = _systemDateTimeProvider.Now();
            var moveInProcessId = CreateProcessId();
            processCoordinator.Register(new MoveInProcess(moveInProcessId, moveInEffectiveDate));

            var unitOfWorkMock = fixture.Freeze<Mock<IUnitOfWork>>();
            var repository = fixture.Create<ProcessCoordinatorRepository>();

            await repository.SaveAsync(processCoordinator);
            await repository.GetByProcessCoordinatorIdAsync(processCoordinator.ProcessCoordinatorId);
        }

        private ProcessId CreateProcessId()
        {
            return new ProcessId(Guid.NewGuid().ToString());
        }

        private ProcessCoordinator Create()
        {
            var meteringPointId = "FakeMeteringPointId";
            var id = new ProcessCoordinatorId(meteringPointId);
            return new ProcessCoordinator(id);
        }
    }
}
