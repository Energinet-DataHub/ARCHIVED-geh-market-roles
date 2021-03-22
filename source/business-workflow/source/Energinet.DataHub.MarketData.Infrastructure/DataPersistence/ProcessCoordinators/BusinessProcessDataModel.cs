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

using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using NodaTime;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.ProcessCoordinators
{
    public class BusinessProcessDataModel
    {
        public BusinessProcessDataModel(int id, string processId, Instant effectiveDate, int state, string processType, int intent, string? suspendedByProcessId)
        {
            Id = id;
            ProcessId = processId;
            EffectiveDate = effectiveDate;
            State = state;
            ProcessType = processType;
            Intent = intent;
            SuspendedByProcessId = suspendedByProcessId;
        }

        public int Id { get; }

        public string ProcessId { get; }

        public Instant EffectiveDate { get; }

        public int State { get; }

        public string ProcessType { get; }

        public int Intent { get; }

        public string? SuspendedByProcessId { get; }
    }
}
