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
using Processing.Domain.MeteringPoints;
using Processing.Infrastructure.Configuration.DataAccess.AccountingPoints;
using Processing.Infrastructure.Configuration.DataAccess.EnergySuppliers;
using Processing.Infrastructure.Configuration.InternalCommands;
using Processing.Infrastructure.Configuration.Outbox;

namespace Processing.Infrastructure.Configuration.DataAccess
{
    public class MarketRolesContext : DbContext
    {
        #nullable disable
        public MarketRolesContext(DbContextOptions<MarketRolesContext> options)
            : base(options)
        {
        }

        public MarketRolesContext()
        {
        }

        public DbSet<Domain.EnergySuppliers.EnergySupplier> EnergySuppliers { get; private set; }

        public DbSet<AccountingPoint> AccountingPoints { get; private set; }

        public DbSet<OutboxMessage> OutboxMessages { get; private set; }

        public DbSet<QueuedInternalCommand> QueuedInternalCommands { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new EnergySupplierEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AccountingPointEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new QueuedInternalCommandEntityConfiguration());
        }
    }
}
