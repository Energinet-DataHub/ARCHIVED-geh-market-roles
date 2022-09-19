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
using System.Threading.Tasks;
using Dapper;
using JetBrains.Annotations;
using NodaTime;
using Processing.Application.Common;
using Processing.Application.Common.Commands;
using Processing.Application.EnergySuppliers;
using Processing.Domain.EnergySuppliers;
using Processing.Infrastructure.Configuration.InternalCommands;
using Xunit;

namespace Processing.IntegrationTests.Application.EnegySuppliers
{
    public class CreateEnergySupplierTests : TestHost
    {
        private readonly InternalCommandProcessor _processor;
        private readonly CommandSchedulerFacade _scheduler;
        private readonly IDbConnectionFactory _connectionFactory;

        public CreateEnergySupplierTests([NotNull] DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _processor = GetService<InternalCommandProcessor>();
            _scheduler = GetService<CommandSchedulerFacade>();
            _connectionFactory = GetService<IDbConnectionFactory>();
        }

        [Fact]
        public async Task Energy_supplier_is_created()
        {
            await EventIsReceivedAndProcessed().ConfigureAwait(false);

            var energySupplier = await GetActor().ConfigureAwait(false);

            Assert.NotNull(energySupplier);
            Assert.Equal(SampleData.EnergySupplierId, energySupplier!.Id.ToString());
            Assert.Equal(SampleData.GlnNumber, energySupplier!.GlnNumber);
        }

        private static CreateEnergySupplier CreateCommand()
        {
            return new CreateEnergySupplier(
                SampleData.EnergySupplierId,
                SampleData.GlnNumber);
        }

        private async Task<EnergySupplier?> GetActor()
        {
            var sql = $"SELECT Id, GlnNumber FROM [dbo].[EnergySuppliers] WHERE Id = '{SampleData.EnergySupplierId}'";
            return await _connectionFactory.GetOpenConnection().QuerySingleOrDefaultAsync<EnergySupplier>(sql).ConfigureAwait(false);
        }

        private async Task EventIsReceivedAndProcessed()
        {
            var command = CreateCommand();
            await Schedule(command).ConfigureAwait(false);
            await ProcessPendingCommands().ConfigureAwait(false);
        }

        private async Task ProcessPendingCommands()
        {
            await _processor.ProcessPendingAsync().ConfigureAwait(false);
        }

        private async Task Schedule(InternalCommand command)
        {
            await _scheduler.EnqueueAsync(command).ConfigureAwait(false);
        }

#pragma warning disable
        public record EnergySupplier(Guid Id, string GlnNumber);
#pragma warning restore
    }
}
