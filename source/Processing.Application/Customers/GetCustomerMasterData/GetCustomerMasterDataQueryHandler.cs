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
using Processing.Application.Common.Queries;

namespace Processing.Application.Customers.GetCustomerMasterData;

public class GetCustomerMasterDataQueryHandler : IQueryHandler<GetCustomerMasterDataQuery, CustomerMasterData>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetCustomerMasterDataQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CustomerMasterData> Handle(GetCustomerMasterDataQuery request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var queryStatement = $"SELECT c.Name, cr.BusinessProcessId AS {nameof(CustomerMasterData.RegisteredByProcessId)}, " +
                             $"CASE " +
                             $"WHEN c.CvrNumber IS NULL THEN c.CprNumber " +
                             $"WHEN c.CprNumber IS NULL THEN c.CvrNumber " +
                             $"END AS CustomerId, " +
                             $"CASE " +
                             $"WHEN c.CvrNumber IS NULL THEN 'CPR' " +
                             $"WHEN c.CprNumber IS NULL THEN 'CVR' " +
                             $"END AS CustomerIdType " +
                                $"FROM [dbo].[Consumers] c " +
                                $"JOIN [dbo].[ConsumerRegistrations] cr ON cr.ConsumerId = c.Id " +
                                $"WHERE cr.BusinessProcessId = @ProcessId";

        var dataModel = await _connectionFactory.GetOpenConnection().QuerySingleAsync<CustomerMasterDataModel>(
            queryStatement,
            new
            {
                ProcessId = request.ProcessId,
            }).ConfigureAwait(false);

        return new CustomerMasterData(dataModel.RegisteredByProcessId, dataModel.CustomerId, dataModel.Name, dataModel.CustomerIdType);
    }
}

public record CustomerMasterDataModel(string Name, Guid RegisteredByProcessId, string CustomerId, string CustomerIdType);
public record CustomerMasterData(Guid RegisteredByProcessId, string CustomerId, string CustomerName, string CustomerIdType);
