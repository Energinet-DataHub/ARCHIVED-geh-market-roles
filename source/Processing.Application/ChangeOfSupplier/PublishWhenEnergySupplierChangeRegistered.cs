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
using Dapper;
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Processing.Application.Common;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints.Events;

namespace Processing.Application.ChangeOfSupplier
{
    public class
        PublishWhenEnergySupplierChangeRegistered : INotificationHandler<EnergySupplierChangeRegistered>
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IEventPublisher _eventPublisher;

        public PublishWhenEnergySupplierChangeRegistered(
            IDbConnectionFactory connectionFactory,
            IEventPublisher eventPublisher)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _eventPublisher = eventPublisher;
        }

        public async Task Handle(
            EnergySupplierChangeRegistered notification,
            CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            var supplierGlnNumber = await GetSupplierGlnNumberAsync(notification.EnergySupplierId)
                .ConfigureAwait(false);
            var integrationEvent = new FutureEnergySupplierChangeRegistered()
            {
                Id = Guid.NewGuid().ToString(),
                AccountingpointId = notification.AccountingPointId.Value.ToString(),
                GsrnNumber = notification.GsrnNumber.Value,
                EnergySupplierGln = supplierGlnNumber,
                EffectiveDate = Timestamp.FromDateTimeOffset(notification.EffectiveDate.ToDateTimeOffset()),
            };

            await _eventPublisher.PublishAsync(integrationEvent).ConfigureAwait(false);
        }

        private async Task<string> GetSupplierGlnNumberAsync(EnergySupplierId energySupplierId)
        {
            var sql = $"SELECT GlnNumber FROM [dbo].[EnergySuppliers] WHERE Id = @EnergySupplierId";
            return await _connectionFactory.GetOpenConnection()
                .QuerySingleOrDefaultAsync<string>(sql, new { EnergySupplierId = energySupplierId.Value })
                .ConfigureAwait(false);
        }
    }
}
