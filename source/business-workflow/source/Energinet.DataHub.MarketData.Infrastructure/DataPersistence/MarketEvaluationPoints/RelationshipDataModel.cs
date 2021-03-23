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
using System.ComponentModel.Design.Serialization;
using NodaTime;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.MarketEvaluationPoints
{
    public class RelationshipDataModel
    {
        public RelationshipDataModel()
        {
        }

#pragma warning disable CA1707
        public RelationshipDataModel(int id, int marketParticipantId, int marketEvaluationPoint_Id, int type, Instant startDate, Instant endDate, string mrid)
        {
            Id = id;
            MarketEvaluationPointId = marketEvaluationPoint_Id;
            Mrid = mrid;
            MarketParticipantId = marketParticipantId;
            Type = type;
            StartDate = startDate;
            EndDate = endDate;
        }

        public RelationshipDataModel(int id, int marketEvaluationPoint_Id, string mrid, int type, Instant startDate, Instant endDate)
        {
            Id = id;
            MarketEvaluationPointId = marketEvaluationPoint_Id;
            Mrid = mrid;
            Type = type;
            StartDate = startDate;
            EndDate = endDate;
        }
#pragma warning restore CA1707

        public int Id { get; set; }

        public int MarketEvaluationPointId { get; set; }

        public string? Mrid { get; set; }

        public int MarketParticipantId { get; set; }

        public int Type { get; set; }

        public Instant StartDate { get; set; }

        public Instant EndDate { get; set; }
    }
}
