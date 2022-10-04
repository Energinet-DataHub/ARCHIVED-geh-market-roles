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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NodaTime;
using Processing.Application.Common;
using Processing.Application.Common.Queries;
using Processing.Domain.Customers;

namespace Processing.Application.Customers.GetCustomerMasterData;

public class GetCustomerMasterDataQueryHandler : IQueryHandler<GetCustomerMasterDataQuery, QueryResult<CustomerMasterData>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetCustomerMasterDataQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<QueryResult<CustomerMasterData>> Handle(GetCustomerMasterDataQuery request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var queryStatement = $"SELECT cr.CustomerName AS {nameof(CustomerMasterData.CustomerName)}, " +
                             $"cr.BusinessProcessId AS {nameof(CustomerMasterData.RegisteredByProcessId)}, " +
                             $"cr.MoveInDate AS {nameof(CustomerMasterData.SupplyStart)}, " +
                             $"cr.CustomerNumber AS CustomerId, " +
                             $"a.ElectricalHeating_EffectiveDate AS {nameof(CustomerMasterData.ElectricalHeatingEffectiveDate)}, " +
                             $"a.GsrnNumber AS {nameof(CustomerMasterData.AccountingPointNumber)} " +
                             $"FROM [dbo].[ConsumerRegistrations] cr " +
                                $"JOIN [dbo].[AccountingPoints] a ON a.Id = cr.AccountingPointId " +
                                $"WHERE cr.BusinessProcessId = @ProcessId";

        var dataModel = await _connectionFactory.GetOpenConnection().QuerySingleOrDefaultAsync<CustomerMasterData>(
            queryStatement,
            new
            {
                ProcessId = request.ProcessId,
            }).ConfigureAwait(false);

        if (dataModel is null)
        {
            return new QueryResult<CustomerMasterData>(
                $"Could not find customer data for process id {request.ProcessId}");
        }

        return new QueryResult<CustomerMasterData>(RemoveCustomerNumberIfPersonal(dataModel));
    }

    private static CustomerMasterData RemoveCustomerNumberIfPersonal(CustomerMasterData input)
    {
        var customerNumber = CustomerNumber.Create(input.CustomerId);
        if (customerNumber.Type == CustomerNumber.CustomerNumberType.Cpr ||
            customerNumber.Type == CustomerNumber.CustomerNumberType.FictionalCpr)
        {
            return new CustomerMasterData(
                input.CustomerName,
                input.RegisteredByProcessId,
                input.SupplyStart,
                string.Empty,
                input.ElectricalHeatingEffectiveDate,
                input.AccountingPointNumber);
        }

        return input;
    }
}

public record CustomerMasterData(
    string CustomerName,
    Guid RegisteredByProcessId,
    Instant SupplyStart,
    string CustomerId,
    Instant? ElectricalHeatingEffectiveDate,
    string AccountingPointNumber);
