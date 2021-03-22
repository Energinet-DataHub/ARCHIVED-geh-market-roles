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
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.ProcessCoordinators
{
    public class ProcessCoordinatorRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessCoordinatorRepository(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task SaveAsync(ProcessCoordinator processCoordinator)
        {
            if (processCoordinator is null)
            {
                throw new ArgumentNullException(nameof(processCoordinator));
            }

            var dataModel = CreateDataModelFrom(processCoordinator);
            IAsyncCommand? command;

            if (dataModel.Version == default) command = new InsertProcessCoordinatorDataModel(dataModel);
            else command = new UpdateProcessCoordinatorDataModel(dataModel);

            return _unitOfWork.ExecuteAsync(command);
        }

        public async Task<ProcessCoordinator> GetByProcessCoordinatorIdAsync(ProcessCoordinatorId processCoordinatorId)
        {
            var dataModel = await _unitOfWork.QueryAsync(new GetProcessCoordinatorByIdQuery(processCoordinatorId.Value));
            return new ProcessCoordinator(processCoordinatorId); // .CreateFrom(new ProcessCoordinatorSnapshot());
        }

        private ProcessCoordinatorDataModel CreateDataModelFrom(ProcessCoordinator processCoordinator)
        {
            var snapshot = processCoordinator.GetSnapshot();
            var businessProcessDataModels = snapshot.BusinessProcesses.Select(
                process => new BusinessProcessDataModel(
                    process.Id,
                    process.ProcessId.Value,
                    process.EffectiveDate,
                    process.State.Id,
                    process.ProcessType.Name,
                    process.ProcessType.Intent.Id,
                    process.SuspendedByProcessId?.Value)).ToList();

            return new ProcessCoordinatorDataModel(
                snapshot.Id,
                snapshot.ProcessCoordinatorId.Value,
                businessProcessDataModels,
                snapshot.Version);
        }

        private void InsertProcessCoordinator(object dataModel)
        {
            throw new NotImplementedException();
        }

        private void UpdateProcessCoordinator(object dataModel)
        {
            throw new NotImplementedException();
        }
    }
}
