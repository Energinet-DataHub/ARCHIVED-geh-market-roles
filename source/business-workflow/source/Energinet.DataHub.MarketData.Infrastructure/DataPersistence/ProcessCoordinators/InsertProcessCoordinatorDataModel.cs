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

using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.ProcessCoordinators
{
    public class InsertProcessCoordinatorDataModel : IAsyncCommand
    {
        private readonly ProcessCoordinatorDataModel _dataModel;

        public InsertProcessCoordinatorDataModel(ProcessCoordinatorDataModel dataModel)
        {
            _dataModel = dataModel;
        }

        public async Task ExecuteNonQueryAsync(
            DbConnection dbConnection,
            DbTransaction? dbTransaction,
            CancellationToken cancellationToken = default)
        {
            var processCoordinator = new CommandDefinition(
                Statements.InsertProcessCoordinator,
                new { processId = _dataModel.ProcessCoordinatorId },
                dbTransaction,
                cancellationToken: cancellationToken);

            var recordId = await dbConnection.ExecuteScalarAsync<int>(processCoordinator);
            var values = _dataModel.BusinessProcesses.Select(v => new
            {
                recordId,
                effectiveDate = v.EffectiveDate,
                state = v.State,
                processType = v.ProcessType,
                intent = v.Intent,
                suspendedByProcessId = v.SuspendedByProcessId,
            });

            var businessProcess = new CommandDefinition(
                Statements.InsertProcessCoordinatorBusinessProcess,
                values);

            await dbConnection.ExecuteAsync(businessProcess);
        }

        private static class Statements
        {
            public const string InsertProcessCoordinator = @"
DECLARE @processsId as NVARCHAR(36);
INSERT INTO dbo.ProcessCoordinators (ProcessCoordinatorId) VALUES (@processsId);
SELECT @@IDENTITY AS RecordId;";

            public const string InsertProcessCoordinatorBusinessProcess = @"
DECLARE @effectiveDate datetime2(7), @state INT, @processType nvarchar(32), @intent INT, @suspendedByProcessId NVARCHAR(36), @suspendedByProcessRecordId INT;
SELECT @suspendedByProcessRecordId = Id FROM dbo.ProcessCoordinators WHERE ProcessCoordinatorId = @suspendedByProcessId;
INSERT INTO dbo.ProcessCoordinatorBusinessProcesses (ProcessCoordinatorId, EffectiveDate, State, ProcessType, Intent, SuspendedByProcessId)
VALUES (@recordId, @effectiveDate, @state, @processType, @intent, @suspendedByProcessRecordId);";
        }
    }
}
