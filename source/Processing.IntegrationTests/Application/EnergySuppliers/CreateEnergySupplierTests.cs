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
using MediatR;
using Processing.Application.Common;
using Processing.Application.EnergySuppliers;
using Processing.IntegrationTests.Fixtures;
using Xunit;

namespace Processing.IntegrationTests.Application.EnergySuppliers
{
    public class CreateEnergySupplierTests : TestBase
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IMediator _mediator;

        public CreateEnergySupplierTests([NotNull] DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _mediator = GetService<IMediator>();
            _connectionFactory = GetService<IDbConnectionFactory>();
        }

        [Fact]
        public async Task Energy_supplier_is_created()
        {
            var command = CreateCommand();
            await _mediator.Send(command).ConfigureAwait(false);

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

#pragma warning disable
        public record EnergySupplier(Guid Id, string GlnNumber);
#pragma warning restore
    }
}
