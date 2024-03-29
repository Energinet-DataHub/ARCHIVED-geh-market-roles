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
using MediatR;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Infrastructure.Transactions.Aggregations;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration;

internal static class AggregationsConfiguration
{
    internal static void Configure(IServiceCollection services, Func<IServiceProvider, IAggregationResults> aggregationResultsBuilder)
    {
        services.AddScoped<AggregationResultMapper>();
        services.AddTransient<IRequestHandler<StartTransaction, Unit>, StartTransactionHandler>();
        services.AddScoped<IAggregationResultForwardingRepository, AggregationResultForwardingRepository>();
        services.AddSingleton<IGridAreaLookup, GridAreaLookup>();
        services.AddTransient<IRequestHandler<RetrieveAggregationResult, Unit>, RetrieveAggregationResultHandler>();
        services.AddSingleton(aggregationResultsBuilder);
    }
}
