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
using System.Text.Json.Serialization;
using Processing.Application.Common.Commands;

namespace Processing.Application.MoveIn.Processing
{
    public class EffectuateConsumerMoveIn
        : InternalCommand
    {
        public EffectuateConsumerMoveIn(Guid accountingPointId, string processId)
        {
            AccountingPointId = accountingPointId;
            ProcessId = processId;
        }

        [JsonConstructor]
        public EffectuateConsumerMoveIn(Guid id, Guid accountingPointId, string processId)
            : this(accountingPointId, processId)
        {
            Id = id;
        }

        public Guid AccountingPointId { get; }

        public string ProcessId { get; }
    }
}
