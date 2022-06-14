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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using MediatR;
using Microsoft.Data.SqlClient;
using NodaTime;
using Processing.Application.ChangeOfSupplier;
using Processing.Application.ChangeOfSupplier.Processing;
using Processing.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Processing.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Processing.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Processing.Domain.Consumers;
using Processing.Domain.MeteringPoints;
using Processing.Infrastructure.Configuration.DataAccess;
using Xunit;
using Xunit.Categories;

namespace Processing.IntegrationTests.Application.ChangeOfSupplier.Processing.Commands
{
    [IntegrationTest]
    public class ChangeSupplierTests : TestHost
    {
        private readonly AccountingPoint _accountingPoint;
        private readonly Domain.EnergySuppliers.EnergySupplier _energySupplier;
        private readonly Domain.EnergySuppliers.EnergySupplier _newEnergySupplier;
        private readonly Consumer _consumer;
        private readonly IMediator _mediator;
        private readonly string _glnNumber = "7495563456235";

        public ChangeSupplierTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _accountingPoint = CreateAccountingPoint();
            _energySupplier = CreateEnergySupplier(Guid.NewGuid(), SampleData.GsrnNumber);
            _newEnergySupplier = CreateEnergySupplier(Guid.NewGuid(), _glnNumber);
            _consumer = CreateConsumer();
            _mediator = GetService<IMediator>();
        }

        [Fact]
        public async Task ChangeSupplier_WhenEffectiveDateIsDue_IsSuccessful()
        {
            var processId = await SimulateProcess().ConfigureAwait(false);

            var command = new ChangeSupplier(_accountingPoint.Id.Value, processId.ToString());
            await GetService<IMediator>().Send(command, CancellationToken.None).ConfigureAwait(false);

            var query = @"SELECT Count(1) FROM SupplierRegistrations WHERE AccountingPointId = @AccountingPointId AND StartOfSupplyDate IS NOT NULL AND EndOfSupplyDate IS NULL";
            await using var sqlCommand = new SqlCommand(query, GetSqlDbConnection());

            sqlCommand.Parameters.Add(new SqlParameter("@AccountingPointId", _accountingPoint.Id.Value));
            sqlCommand.Parameters.Add(new SqlParameter("@EnergySupplierId", _energySupplier.EnergySupplierId.Value));

            var result = await sqlCommand.ExecuteScalarAsync().ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RequestChangeOfSupplier_IsSuccessful_FutureSupplier_IntegrationEventsIsPublished()
        {
            await RequestFutureChangeOfSupplierProcess().ConfigureAwait(false);

            AssertOutboxMessage<FutureEnergySupplierChangeRegistered>();
        }

        private async Task<Guid> SimulateProcess()
        {
            await SetConsumerMovedIn().ConfigureAwait(false);

            var processId = await RequestChangeOfSupplier().ConfigureAwait(false);

            await _mediator.Send(new ForwardMeteringPointDetails(_accountingPoint.Id.Value, processId)).ConfigureAwait(false);
            await _mediator.Send(new ForwardConsumerDetails(_accountingPoint.Id.Value, processId)).ConfigureAwait(false);
            await _mediator.Send(new NotifyCurrentSupplier(_accountingPoint.Id.Value, processId)).ConfigureAwait(false);

            return processId;
        }

        private async Task RequestFutureChangeOfSupplierProcess()
        {
            await SetConsumerMovedIn().ConfigureAwait(false);
            await RequestChangeOfSupplierInFuture().ConfigureAwait(false);
        }

        private async Task<Guid> RequestChangeOfSupplier()
        {
            var result = await _mediator.Send(new RequestChangeOfSupplier(
                _glnNumber,
                _consumer.CprNumber?.Value ?? throw new InvalidOperationException("CprNumber was supposed to have a value"),
                string.Empty,
                _accountingPoint.GsrnNumber.Value,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(1)).ToString())).ConfigureAwait(false);

            if (result.ProcessId is null)
            {
                throw new InvalidOperationException("Process id is null");
            }

            return Guid.Parse(result.ProcessId);
        }

        private async Task RequestChangeOfSupplierInFuture()
        {
            await _mediator.Send(new RequestChangeOfSupplier(
                _newEnergySupplier.GlnNumber.Value,
                _consumer.CprNumber?.Value ?? throw new InvalidOperationException("CprNumber was supposed to have a value"),
                string.Empty,
                _accountingPoint.GsrnNumber.Value,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(80)).ToString())).ConfigureAwait(false);
        }

        private async Task SetConsumerMovedIn()
        {
            SetConsumerMovedIn(_accountingPoint, _consumer.ConsumerId, _energySupplier.EnergySupplierId);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        }
    }
}
