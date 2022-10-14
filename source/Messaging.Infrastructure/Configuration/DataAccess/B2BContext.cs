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
using Contracts.BusinessRequests.MoveIn;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess.Outgoing;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Infrastructure.Transactions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Messaging.Infrastructure.Configuration.DataAccess
{
    public class B2BContext : DbContext
    {
        private readonly ISerializer _serializer;

        #nullable disable
        public B2BContext(DbContextOptions<B2BContext> options, ISerializer serializer)
            : base(options)
        {
            _serializer = serializer;
        }

        public B2BContext()
        {
        }

        public DbSet<MoveInTransaction> Transactions { get; private set; }

        public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

        public DbSet<QueuedInternalCommand> QueuedInternalCommands { get; private set; }

        public DbSet<MarketEvaluationPoint> MarketEvaluationPoints { get; private set; }

        public DbSet<ConfirmRequestChangeOfSupplierMessage> ConfirmRequestChangeOfSupplierMessages { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new MoveInTransactionEntityConfiguration(_serializer));
            modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new QueuedInternalCommandEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MarketEvaluationPointEntityConfiguration());

            modelBuilder.Entity<ConfirmRequestChangeOfSupplierMessage>()
                .Ignore(x => x.MarketActivityRecord)
                .Property(x => x.MarketActivityRecordPayload);
            // .Property(entity => entity.MarketActivityRecord)
            // .HasColumnName("MarketActivityRecordPayload")
            // .HasConversion(
            //     toDbValue => JsonConvert.SerializeObject(toDbValue),
            //     fromDbValue => JsonConvert.DeserializeObject<MarketActivityRecord>(fromDbValue));
            // .OwnsOne(x => x.MarketActivityRecord, model =>
            // {
            //     model.Property(x => x.Id)
            //         .HasColumnName("MarketActivityRecord_OriginalTransactionId");
            //     model.Property(x => x.OriginalTransactionId)
            //         .HasColumnName("MarketActivityRecord_Id");
            //     model.Property(x => x.MarketEvaluationPointId)
            //         .HasColumnName("MarketActivityRecord_MarketEvaluationPointId");
            // });
        }
    }
}
