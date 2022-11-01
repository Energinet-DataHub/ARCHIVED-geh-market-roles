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
using Processing.Application.ChangeCustomerCharacteristics;
using Processing.Application.Common;
using Processing.Domain.Customers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Errors;

namespace Processing.Application.ChangeCustomerMasterData;

public class ChangeCustomerMasterDataRequestHandler : IBusinessRequestHandler<ChangeCustomerMasterDataRequest>
{
    private readonly IAccountingPointRepository _accountingPointRepository;

    public ChangeCustomerMasterDataRequestHandler(IAccountingPointRepository accountingPointRepository)
    {
        _accountingPointRepository = accountingPointRepository;
    }

    public async Task<BusinessProcessResult> Handle(ChangeCustomerMasterDataRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var accountingPoint = await
            _accountingPointRepository.GetByGsrnNumberAsync(GsrnNumber.Create(request.AccountingPointNumber)).ConfigureAwait(false);

        if (accountingPoint is null)
        {
            return BusinessProcessResult.Fail(new UnknownAccountingPoint());
        }

        if (request.Customer != null)
        {
            var customer = Domain.Customers.Customer.Create(CustomerNumber.Create(request.Customer.Number), request.Customer.Name);
            accountingPoint.UpdateConsumerCustomer(BusinessProcessId.Create(request.ProcessId), customer);
        }

        if (request.SecondCustomer != null)
        {
            var secondCustomer = Domain.Customers.Customer.Create(
                CustomerNumber.Create(request.SecondCustomer.Number),
                request.SecondCustomer.Name);
            accountingPoint.UpdateConsumerSecondCustomer(BusinessProcessId.Create(request.ProcessId), secondCustomer);
        }

        return BusinessProcessResult.Ok(request.ProcessId);
    }
}
