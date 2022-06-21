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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processing.Domain.EnergySuppliers;

namespace Processing.Infrastructure.Configuration.DataAccess.EnergySuppliers
{
    internal class EnergySupplierEntityConfiguration : IEntityTypeConfiguration<Domain.EnergySuppliers.EnergySupplier>
    {
        public void Configure(EntityTypeBuilder<Domain.EnergySuppliers.EnergySupplier> builder)
        {
            builder.ToTable("EnergySuppliers", "dbo");
            builder.HasKey(x => x.EnergySupplierId);
            builder.Property(x => x.EnergySupplierId)
                .HasColumnName("Id")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new EnergySupplierId(fromDbValue));

            builder.Property(x => x.GlnNumber)
                .HasColumnName("GlnNumber")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new GlnNumber(fromDbValue));

            builder.Property<DateTime>("RowVersion")
                .HasColumnName("RowVersion")
                .HasColumnType("timestamp")
                .IsRowVersion();

            builder.Ignore(x => x.DomainEvents);
        }
    }
}
