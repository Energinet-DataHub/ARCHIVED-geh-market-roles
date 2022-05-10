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
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NodaTime.Text;
using Processing.Application.Common;
using Processing.Application.Common.Validation;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Errors;
using Processing.Domain.SeedWork;

namespace Processing.Application.MoveIn
{
    public class MoveInRequestHandler : IBusinessRequestHandler<RequestMoveIn>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;
        private readonly IEnergySupplierRepository _energySupplierRepository;
        private readonly IConsumerRepository _consumerRepository;

        public MoveInRequestHandler(
            IAccountingPointRepository accountingPointRepository,
            IEnergySupplierRepository energySupplierRepository,
            IConsumerRepository consumerRepository)
        {
            _accountingPointRepository = accountingPointRepository ?? throw new ArgumentNullException(nameof(accountingPointRepository));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
            _consumerRepository = consumerRepository ?? throw new ArgumentNullException(nameof(consumerRepository));
        }

        public async Task<BusinessProcessResult> Handle(RequestMoveIn request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var accountingPoint = await _accountingPointRepository.GetByGsrnNumberAsync(GsrnNumber.Create(request.AccountingPointGsrnNumber)).ConfigureAwait(false);
            if (accountingPoint is null)
            {
                return BusinessProcessResult.Fail(request.TransactionId, new UnknownAccountingPoint(request.AccountingPointGsrnNumber));
            }

            var energySupplier = await _energySupplierRepository.GetByGlnNumberAsync(new GlnNumber(request.EnergySupplierGlnNumber)).ConfigureAwait(false);

            var validationResult = Validate(energySupplier!, accountingPoint!, request);
            if (validationResult.Success == false)
            {
                return validationResult;
            }

            var businessRulesResult = CheckBusinessRules(accountingPoint!, request);
            if (!businessRulesResult.Success)
            {
                return businessRulesResult;
            }

            var consumer = await GetOrCreateConsumerAsync(request).ConfigureAwait(false);

            var startDate = Instant.FromDateTimeOffset(DateTimeOffset.Parse(request.MoveInDate, CultureInfo.InvariantCulture));

            accountingPoint.AcceptConsumerMoveIn(consumer.ConsumerId, energySupplier!.EnergySupplierId, startDate, Transaction.Create(request.TransactionId));
            return BusinessProcessResult.Ok(request.TransactionId);
        }

        private static BusinessProcessResult Validate(EnergySupplier energySupplier, global::Processing.Domain.MeteringPoints.AccountingPoint accountingPoint, RequestMoveIn request)
        {
            var rules = new List<IBusinessRule>()
            {
                new EnergySupplierMustBeKnownRule(energySupplier, request.EnergySupplierGlnNumber),
                new MeteringPointMustBeKnownRule(accountingPoint, request.AccountingPointGsrnNumber),
            };

            return new BusinessProcessResult(request.TransactionId, rules);
        }

        private static BusinessProcessResult CheckBusinessRules(global::Processing.Domain.MeteringPoints.AccountingPoint accountingPoint, RequestMoveIn request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var moveInDate = InstantPattern.General.Parse(request.MoveInDate).Value;

            var validationResult = accountingPoint.ConsumerMoveInAcceptable(moveInDate);

            return new BusinessProcessResult(request.TransactionId, validationResult.Errors);
        }

        private async Task<Consumer> GetOrCreateConsumerAsync(RequestMoveIn request)
        {
            Consumer? consumer;
            if (string.IsNullOrWhiteSpace(request.SocialSecurityNumber) == false)
            {
                consumer = await _consumerRepository.GetBySSNAsync(CprNumber.Create(request.SocialSecurityNumber)).ConfigureAwait(false);
            }
            else
            {
                consumer = await _consumerRepository.GetByVATNumberAsync(CvrNumber.Create(request.VATNumber)).ConfigureAwait(false);
            }

            return consumer ?? CreateConsumer(request);
        }

        private Consumer CreateConsumer(RequestMoveIn request)
        {
            var consumerName = ConsumerName.Create(request.ConsumerName);
            Consumer consumer = string.IsNullOrWhiteSpace(request.SocialSecurityNumber) == false
                ? new Consumer(ConsumerId.New(), CprNumber.Create(request.SocialSecurityNumber), consumerName)
                : new Consumer(ConsumerId.New(), CvrNumber.Create(request.VATNumber), consumerName);
            _consumerRepository.Add(consumer);
            return consumer;
        }
    }
}
