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
using System.Threading.Tasks;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Infrastructure.Transactions.Aggregations;

internal class AggregationResultForwardingRepository : IAggregationResultForwardingRepository
{
    private readonly B2BContext _context;

    public AggregationResultForwardingRepository(B2BContext context)
    {
        _context = context;
    }

    public void Add(AggregationResultForwarding transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _context.AggregatedTimeSeriesTransactions.Add(transaction);
    }

    public Task<AggregationResultForwarding?> GetAsync(TransactionId transactionId)
    {
        ArgumentNullException.ThrowIfNull(transactionId);
        return _context.AggregatedTimeSeriesTransactions.Include("_messages").FirstOrDefaultAsync(t => t.Id == transactionId);
    }
}
