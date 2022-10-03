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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Processing.Application.Common;
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Domain.Common;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.EnergySuppliers.Errors;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Errors;
using Processing.Domain.SeedWork;

namespace Processing.Application.MoveIn
{
    public class MoveInRequestHandler : IBusinessRequestHandler<MoveInRequest>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;
        private readonly IEnergySupplierRepository _energySupplierRepository;
        private readonly IConsumerRepository _consumerRepository;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly CustomerMoveIn _customerMoveInProcess;

        public MoveInRequestHandler(
            IAccountingPointRepository accountingPointRepository,
            IEnergySupplierRepository energySupplierRepository,
            IConsumerRepository consumerRepository,
            ISystemDateTimeProvider systemDateTimeProvider,
            EffectiveDatePolicy effectiveDatePolicy)
        {
            _accountingPointRepository = accountingPointRepository ?? throw new ArgumentNullException(nameof(accountingPointRepository));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
            _consumerRepository = consumerRepository ?? throw new ArgumentNullException(nameof(consumerRepository));
            _systemDateTimeProvider = systemDateTimeProvider;
            _customerMoveInProcess = new CustomerMoveIn(effectiveDatePolicy);
        }

        public async Task<BusinessProcessResult> Handle(MoveInRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var accountingPoint = await _accountingPointRepository.GetByGsrnNumberAsync(GsrnNumber.Create(request.AccountingPointNumber)).ConfigureAwait(false);
            if (accountingPoint is null)
            {
                return BusinessProcessResult.Fail(new UnknownAccountingPoint());
            }

            var energySupplier = await _energySupplierRepository.GetByGlnNumberAsync(new GlnNumber(request.EnergySupplierNumber)).ConfigureAwait(false);
            if (energySupplier is null)
            {
                return BusinessProcessResult.Fail(new UnknownEnergySupplier());
            }

            var consumerMovesInOn = EffectiveDate.Create(request.EffectiveDate);
            var checkResult = _customerMoveInProcess.CanStartProcess(accountingPoint, consumerMovesInOn, _systemDateTimeProvider.Now());

            if (!checkResult.Success)
            {
                return BusinessProcessResult.Fail(checkResult.Errors.ToArray());
            }

            var consumer = await GetOrCreateConsumerAsync(request).ConfigureAwait(false);

            var businessProcessId = BusinessProcessId.New();
            _customerMoveInProcess.StartProcess(
                accountingPoint,
                consumer,
                energySupplier,
                consumerMovesInOn,
                _systemDateTimeProvider.Now(),
                businessProcessId,
                CreateCustomer(request));

            return BusinessProcessResult.Ok(businessProcessId.Value.ToString());
        }

        private static Domain.Consumers.Customer CreateCustomer(MoveInRequest request)
        {
            return Domain.Consumers.Customer.Create(CustomerNumber.Create(request.Customer.Number), request.Customer.Name);
        }

        private async Task<Domain.Consumers.Consumer> GetOrCreateConsumerAsync(MoveInRequest moveInRequest)
        {
            Domain.Consumers.Consumer? consumer;
            if (moveInRequest.Customer.Type.Equals("CPR", StringComparison.OrdinalIgnoreCase))
            {
                consumer = await _consumerRepository.GetBySSNAsync(CprNumber.Create(moveInRequest.Customer.Number)).ConfigureAwait(false);
            }
            else
            {
                consumer = await _consumerRepository.GetByVATNumberAsync(CvrNumber.Create(moveInRequest.Customer.Number)).ConfigureAwait(false);
            }

            return consumer ?? CreateConsumer(moveInRequest);
        }

        private Domain.Consumers.Consumer CreateConsumer(MoveInRequest moveInRequest)
        {
            var consumerName = ConsumerName.Create(moveInRequest.Customer.Name);
            var consumer = moveInRequest.Customer.Type.Equals("CPR", StringComparison.OrdinalIgnoreCase)
                ? new Domain.Consumers.Consumer(ConsumerId.New(), CprNumber.Create(moveInRequest.Customer.Number), consumerName)
                : new Domain.Consumers.Consumer(ConsumerId.New(), CvrNumber.Create(moveInRequest.Customer.Number), consumerName);
            _consumerRepository.Add(consumer);
            return consumer;
        }
    }
}
