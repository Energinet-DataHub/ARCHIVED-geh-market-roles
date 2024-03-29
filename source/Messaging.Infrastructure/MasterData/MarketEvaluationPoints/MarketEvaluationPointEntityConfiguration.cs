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
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.MasterData.MarketEvaluationPoints;

public class MarketEvaluationPointEntityConfiguration : IEntityTypeConfiguration<MarketEvaluationPoint>
{
    public void Configure(EntityTypeBuilder<MarketEvaluationPoint> builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        builder.ToTable("MarketEvaluationPoints", "b2b");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EnergySupplierNumber)
            .HasConversion(
                toDbValue => toDbValue!.Value,
                fromDbValue => ActorNumber.Create(fromDbValue));
        builder.Property(x => x.MarketEvaluationPointNumber);
        builder.Property(x => x.GridOperatorId);
    }
}
