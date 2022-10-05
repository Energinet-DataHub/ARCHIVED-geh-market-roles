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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Processing.Application.Common;
using Processing.Domain.BusinessProcesses.MoveIn;
using Processing.Domain.Common;
using Processing.Domain.Customers;
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
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly CustomerMoveIn _customerMoveInProcess;

        public MoveInRequestHandler(
            IAccountingPointRepository accountingPointRepository,
            IEnergySupplierRepository energySupplierRepository,
            ISystemDateTimeProvider systemDateTimeProvider,
            EffectiveDatePolicy effectiveDatePolicy)
        {
            _accountingPointRepository = accountingPointRepository ?? throw new ArgumentNullException(nameof(accountingPointRepository));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
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
            var customer = CreateCustomer(request);
            var checkResult = _customerMoveInProcess.CanStartProcess(accountingPoint, consumerMovesInOn, _systemDateTimeProvider.Now(), customer);

            if (!checkResult.Success)
            {
                return BusinessProcessResult.Fail(checkResult.Errors.ToArray());
            }

            var businessProcessId = BusinessProcessId.New();
            _customerMoveInProcess.StartProcess(
                accountingPoint,
                energySupplier,
                consumerMovesInOn,
                _systemDateTimeProvider.Now(),
                businessProcessId,
                customer);

            return BusinessProcessResult.Ok(businessProcessId.Value.ToString());
        }

        private static Domain.Customers.Customer CreateCustomer(MoveInRequest request)
        {
            return Domain.Customers.Customer.Create(CustomerNumber.Create(request.Customer.Number), request.Customer.Name);
        }
    }
}
