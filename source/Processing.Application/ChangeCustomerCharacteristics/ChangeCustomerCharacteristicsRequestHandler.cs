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
using Processing.Application.Common;
using Processing.Domain.MeteringPoints;

namespace Processing.Application.ChangeCustomerCharacteristics;

public class ChangeCustomerCharacteristicsRequestHandler : IBusinessRequestHandler<ChangeCustomerCharacteristicsRequest>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ChangeCustomerCharacteristicsRequestHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<BusinessProcessResult> Handle(ChangeCustomerCharacteristicsRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var moveInTransaction = await GetMoveInTransactionByProcessIdAsync(request.ProcessId)
            .ConfigureAwait(false);

        return BusinessProcessResult.Ok(request.ProcessId.Value.ToString());
    }

    private async Task<MoveInTransaction> GetMoveInTransactionByProcessIdAsync(BusinessProcessId processId)
    {
        var query = $"SELECT m.ProcessId AS {nameof(MoveInTransaction.ProcessId)}, " +
                    $"m.CustomerMasterData AS {nameof(MoveInTransaction.CustomerMasterData)} " +
                    $"FROM [b2b].MoveInTransactions m " +
                    $"WHERE m.ProcessId = @ProcessId";

        var dataModel = await _dbConnectionFactory.GetOpenConnection().QuerySingleOrDefaultAsync<MoveInTransaction>(
            query,
            new { ProcessId = processId.Value, }).ConfigureAwait(false);

        return dataModel;
    }
}

public record MoveInTransaction(
    string ProcessId,
    string CustomerMasterData);
