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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.UpdateCustomer;

namespace Messaging.Application.Transactions.UpdateCustomer;

public class UpdateCustomerMasterDataHandler : IRequestHandler<UpdateCustomerMasterData, Unit>
{
    private readonly IUpdateCustomerMasterDataTransactionRepository _transactionRepository;

    public UpdateCustomerMasterDataHandler(IUpdateCustomerMasterDataTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public Task<Unit> Handle(UpdateCustomerMasterData request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = new UpdateCustomerMasterDataTransaction(TransactionId.Create(request.TransactionId));
        _transactionRepository.Add(transaction);
        return Task.FromResult<Unit>(Unit.Value);
    }
}
