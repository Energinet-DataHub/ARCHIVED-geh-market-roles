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

namespace Processing.Application.AccountingPoint.GetCurrentSupplierDetails;

public class GetCurrentSupplierDetailsQueryHandler : IQueryHandler<GetCurrentSupplierDetailsQuery, Result>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetCurrentSupplierDetailsQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result> Handle(GetCurrentSupplierDetailsQuery request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var selectStatement =
            $"SELECT r.StartOfSupplyDate AS {nameof(EnergySupplier.StartOfSupplyDate)} , e.GlnNumber AS {nameof(EnergySupplier.EnergySupplierNumber)} " +
            $"FROM [dbo].[SupplierRegistrations] r JOIN AccountingPoints a ON r.AccountingPointId = a.Id " +
            $"JOIN EnergySuppliers e ON e.Id = r.EnergySupplierId " +
            $"WHERE a.GsrnNumber = @GsrnNumber AND r.EndOfSupplyDate IS NULL";

        var dataModel = await _connectionFactory.GetOpenConnection().QuerySingleAsync<EnergySupplier>(
                selectStatement, new
            {
                GsrnNumber = request.AccountingPointNumber,
            })
            .ConfigureAwait(false);

        return new Result(dataModel);
    }
}
