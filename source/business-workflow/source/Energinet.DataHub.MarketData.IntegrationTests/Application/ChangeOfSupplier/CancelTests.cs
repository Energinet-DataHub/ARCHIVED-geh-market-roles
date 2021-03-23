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
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Cancel;
using Energinet.DataHub.MarketData.Domain.Customers;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.IntegrationTests.Application.ChangeOfSupplier
{
    public class CancelTests
    : TestBase
    {
        [Fact]
        public async Task Cancel_WhenGsrnNumberIsUnknown_IsRejected()
        {
            var supplierGlnNumber = "5790000555550";
            var meteringPointGsrnNumber = "571234567891234568";

            var command = new CancelPendingRegistration()
            {
                BalanceSupplierId = supplierGlnNumber,
                TransactionId = "Fake",
                MeteringPointId = meteringPointGsrnNumber,
                RegistrationId = "Fake",
            };

            await Mediator.Send(command, CancellationToken.None).ConfigureAwait(false);

            var publishedActorMessage = await GetLastMessageFromOutboxAsync<CancelPendingRegistrationRejected>().ConfigureAwait(false);
            Assert.Equal(command.MeteringPointId, publishedActorMessage.MeteringPointId);
            Assert.Equal(command.TransactionId, publishedActorMessage.TransactionId);
            Assert.Contains(publishedActorMessage.ReasonCodes, e => e.Equals("E10"));
        }

        [Fact]
        public async Task Cancel_WhenBalanceSupplierIsNotTheSameAsTheOriginalRequester_IsRejected()
        {
            var supplierGlnNumber = "5790000555550";
            var meteringPointGsrnNumber = "571234567891234568";
            await Seed(supplierGlnNumber, meteringPointGsrnNumber);

            var command = new CancelPendingRegistration()
            {
                BalanceSupplierId = supplierGlnNumber,
                TransactionId = "Fake",
                MeteringPointId = meteringPointGsrnNumber,
                RegistrationId = "Fake",
            };

            await Mediator.Send(command, CancellationToken.None);

            var publishedActorMessage = await GetLastMessageFromOutboxAsync<CancelPendingRegistrationRejected>().ConfigureAwait(false);
            Assert.Equal(command.MeteringPointId, publishedActorMessage.MeteringPointId);
            Assert.Equal(command.TransactionId, publishedActorMessage.TransactionId);
            Assert.Contains(publishedActorMessage.ReasonCodes, e => e.Equals("E16"));
        }

        private async Task Seed(string energySupplierGlnNumber, string meteringPointGsrnNumber)
        {
            //TODO: Need to separate customers from energy suppliers - This does not make any sense at all
            var customerId = "Unknown";
            var customer = new EnergySupplier(new GlnNumber(customerId));
            SupplierRepository.Add(customer);

            var energySupplierGln = new GlnNumber(energySupplierGlnNumber);
            var energySupplier = new EnergySupplier(energySupplierGln);
            SupplierRepository.Add(energySupplier);

            await UnitOfWork.CommitAsync().ConfigureAwait(false);

            var meteringPoint =
                MeteringPoint.CreateProduction(
                    GsrnNumber.Create(meteringPointGsrnNumber), true);

            var systemTimeProvider = ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
            var registrationId = new ProcessId(Guid.NewGuid().ToString());
            meteringPoint.RegisterMoveIn(registrationId, new CustomerId(customer.GlnNumber.Value), energySupplierGln, systemTimeProvider.Now().Minus(Duration.FromDays(365)));
            meteringPoint.ActivateMoveIn(registrationId);
            MeteringPointRepository.Add(meteringPoint);
            await UnitOfWork.CommitAsync().ConfigureAwait(false);
        }
    }
}
