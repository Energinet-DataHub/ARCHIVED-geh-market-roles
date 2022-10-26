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
using Processing.Application.Common;
using Processing.Domain.Customers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Errors;

namespace Processing.Application.ChangeCustomerCharacteristics;

public class ChangeCustomerCharacteristicsRequestHandler : IBusinessRequestHandler<ChangeCustomerCharacteristicsRequest>
{
    private readonly IAccountingPointRepository _accountingPointRepository;

    public ChangeCustomerCharacteristicsRequestHandler(IAccountingPointRepository accountingPointRepository)
    {
        _accountingPointRepository = accountingPointRepository;
    }

    public async Task<BusinessProcessResult> Handle(ChangeCustomerCharacteristicsRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var accountingPoint = await
            _accountingPointRepository.GetByGsrnNumberAsync(GsrnNumber.Create(request.AccountingPointId)).ConfigureAwait(false);

        if (accountingPoint is null)
        {
            return BusinessProcessResult.Fail(new UnknownAccountingPoint());
        }

        var customer = Domain.Customers.Customer.Create(CustomerNumber.Create(request.Customer.Number), request.Customer.Name);

        accountingPoint.UpdateConsumerCustomer(BusinessProcessId.Create(request.ProcessId), customer);

        return BusinessProcessResult.Ok(request.ProcessId);
    }
}
