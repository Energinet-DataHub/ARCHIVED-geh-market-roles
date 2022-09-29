﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Text;
using System.Text.Json.Serialization;
using Messaging.Application.Configuration.Commands.Commands;

namespace Messaging.Application.MasterData.MarketEvaluationPoints;

public class CreateMarketEvaluationPoint : InternalCommand
{
    [JsonConstructor]
    public CreateMarketEvaluationPoint(
        string marketEvaluationPointNumber,
        string meteringPointId,
        Guid gridOperatorId,
        string energySupplierNumber = "")
    {
        MarketEvaluationPointNumber = marketEvaluationPointNumber;
        MeteringPointId = meteringPointId;
        GridOperatorId = gridOperatorId;
        EnergySupplierNumber = energySupplierNumber;
    }

    public string MarketEvaluationPointNumber { get; }

    public string MeteringPointId { get; }

    public Guid GridOperatorId { get; }

    public string EnergySupplierNumber { get; }
}