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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Transactions.AggregatedTimeSeries;
using Messaging.Domain.Transactions.AggregatedTimeSeries;

namespace Messaging.Infrastructure.Transactions.AggregatedTimeSeries;

public class FakeAggregatedTimeSeriesResults : IAggregatedTimeSeriesResults
{
    private readonly Dictionary<Guid, AggregatedTimeSeriesResult> _results = new();

    public Task<AggregatedTimeSeriesResult> GetResultAsync(Guid resultId)
    {
        return Task.FromResult(_results[resultId]);
    }

    #pragma warning disable
    public void Add(Guid resultId, TimeSeries timeSeries)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);
        var points = timeSeries.Points.Select(point =>
            new Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point(
                point.Position,
                point.Quantity,
                point.Quality));
        var gridArea = new GridArea(points.ToList());
        var result = new AggregatedTimeSeriesResult(resultId, new List<GridArea>()
        {
            gridArea,
        });

        _results.Add(resultId, result);
    }
}
