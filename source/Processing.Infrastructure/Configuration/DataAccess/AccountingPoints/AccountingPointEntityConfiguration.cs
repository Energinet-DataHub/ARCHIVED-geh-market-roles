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
using Processing.Domain.Common;
using Processing.Domain.Customers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.SeedWork;

namespace Processing.Infrastructure.Configuration.DataAccess.AccountingPoints
{
    public class AccountingPointEntityConfiguration : IEntityTypeConfiguration<AccountingPoint>
    {
        public void Configure(EntityTypeBuilder<AccountingPoint> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.ToTable("AccountingPoints", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new AccountingPointId(fromDbValue));

            builder.Property(x => x.GsrnNumber)
                .HasColumnName("GsrnNumber")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => GsrnNumber.Create(fromDbValue));

            builder.Property<bool>("_isProductionObligated")
                .HasColumnName("ProductionObligated");

            builder.Property<MeteringPointType>("_meteringPointType")
                .HasColumnName("Type")
                .HasConversion(toDbValue => toDbValue.Id, fromDbValue => EnumerationType.FromValue<MeteringPointType>(fromDbValue));

            builder.Property<PhysicalState>("_physicalState")
                .HasColumnName("PhysicalState")
                .HasConversion(toDbValue => toDbValue.Id, fromDbValue => EnumerationType.FromValue<PhysicalState>(fromDbValue));

            builder.Property<ElectricalHeating>("_electricalHeating")
                .HasColumnName("ElectricalHeating_EffectiveDate")
                .HasConversion(
                    toDbValue => toDbValue.EffectiveDate.DateInUtc,
                    fromDbValue => ElectricalHeating.Create(EffectiveDate.Create(fromDbValue.ToDateTimeUtc())));

            builder.OwnsMany<BusinessProcess>("_businessProcesses", x =>
            {
                x.Property("AccountingPointId")
                    .HasColumnType("uniqueidentifier")
                    .HasColumnName("AccountingPointId");
                x.WithOwner()
                    .HasPrincipalKey(y => y.Id)
                    .HasForeignKey("AccountingPointId");

                x.ToTable("BusinessProcesses", "dbo");
                x.HasKey(y => y.BusinessProcessId);
                x.Property(y => y.BusinessProcessId)
                    .HasColumnName("Id")
                    .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new BusinessProcessId(fromDbValue));
                x.Property(y => y.Status)
                    .HasColumnName("Status")
                    .HasConversion(toDbValue => toDbValue.Id, fromDbValue => EnumerationType.FromValue<BusinessProcessStatus>(fromDbValue));
                x.Property(y => y.EffectiveDate)
                    .HasColumnName("EffectiveDate");
                x.Property(y => y.ProcessType)
                    .HasColumnName("ProcessType")
                    .HasConversion(toDbValue => toDbValue.Id, fromDbValue => EnumerationType.FromValue<BusinessProcessType>(fromDbValue));
                x.Ignore(y => y.DomainEvents);
            });

            builder.OwnsMany<ConsumerRegistration>("_consumerRegistrations", y =>
            {
                y.Property("AccountingPointId")
                    .HasColumnName("AccountingPointId");
                y.ToTable("ConsumerRegistrations", "dbo");
                y.HasKey(z => z.Id);
                y.Property(z => z.Id)
                    .HasColumnName("Id")
                    .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new ConsumerRegistrationId(fromDbValue));
                y.Property(z => z.BusinessProcessId)
                    .HasColumnName("BusinessProcessId")
                    .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new BusinessProcessId(fromDbValue));
                y.Property(z => z.MoveInDate)
                    .HasColumnName("MoveInDate");
                y.OwnsOne<Customer>(x => x.Customer, b =>
                {
                    b.Property(n => n.Number)
                        .HasColumnName("CustomerNumber")
                        .HasConversion(
                            toDbValue => toDbValue.Value,
                            fromDbValue => CustomerNumber.Create(fromDbValue));
                    b.Property(n => n.Name)
                        .HasColumnName("CustomerName");
                });
                y.OwnsOne<Customer>(x => x.SecondCustomer, b =>
                {
                    b.Property(n => n.Number)
                        .HasColumnName("SecondCustomerNumber")
                        .HasConversion(
                            toDbValue => toDbValue.Value,
                            fromDbValue => CustomerNumber.Create(fromDbValue));
                    b.Property(n => n.Name)
                        .HasColumnName("SecondCustomerName");
                });
                y.Ignore(z => z.DomainEvents);
            });

            builder.OwnsMany<SupplierRegistration>("_supplierRegistrations", y =>
            {
                y.Property("AccountingPointId")
                    .HasColumnName("AccountingPointId")
                    .HasColumnType("uniqueidentifier");
                y.WithOwner()
                    .HasForeignKey("AccountingPointId")
                    .HasPrincipalKey(z => z.Id);

                y.WithOwner();
                y.ToTable("SupplierRegistrations", "dbo");
                y.HasKey(z => z.Id);
                y.Property(z => z.Id)
                    .HasColumnName("Id")
                    .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new SupplierRegistrationId(fromDbValue));
                y.Property(z => z.EnergySupplierId)
                    .HasColumnName("EnergySupplierId")
                    .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new EnergySupplierId(fromDbValue));
                y.Property(z => z.BusinessProcessId)
                    .HasColumnName("BusinessProcessId")
                    .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new BusinessProcessId(fromDbValue));
                y.Property(z => z.StartOfSupplyDate)
                    .HasColumnName("StartOfSupplyDate");
                y.Property(z => z.EndOfSupplyDate)
                    .HasColumnName("EndOfSupplyDate");

                y.Ignore(z => z.DomainEvents);
            });

            builder.Ignore(x => x.DomainEvents);

            builder.Property<DateTime>("RowVersion")
                .HasColumnName("RowVersion")
                .HasColumnType("timestamp")
                .IsRowVersion();
        }
    }
}
