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
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Processing.Application.Common.TimeEvents;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.Configuration.SystemTime;

namespace Processing.Api.Configuration
{
    public class SystemTimer
    {
        private readonly IMediator _mediator;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public SystemTimer(IMediator mediator, ISystemDateTimeProvider systemDateTimeProvider)
        {
            _mediator = mediator;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        [Function("RaiseTenSecondsHasPassedEvent")]
        public Task OnTenSecondsHasPassedAsync([TimerTrigger("%RAISE_TIME_HAS_PASSED_EVENT_SCHEDULE%")] TimerInfo timerTimerInfo, FunctionContext context)
        {
            LogInfo(timerTimerInfo, context, "RaiseTimeHasPassedEvent");
            return _mediator.Publish(new TenSecondsHasPassed(_systemDateTimeProvider.Now()));
        }

        [Function("RaiseDayHasPassedEvent")]
        public Task OnDayHasPassedAsync([TimerTrigger("0 0 0 * * *")] TimerInfo timerTimerInfo, FunctionContext context)
        {
            LogInfo(timerTimerInfo, context, "RaiseDayHasPassedEvent");
            return _mediator.Publish(new DayHasPassed(_systemDateTimeProvider.Now()));
        }

        private static void LogInfo(TimerInfo timerTimerInfo, FunctionContext context, string functionName)
        {
            var logger = context.GetLogger(functionName);
            logger.LogInformation($"System timer trigger at: {DateTime.Now}");
            logger.LogInformation($"Next timer schedule at: {timerTimerInfo?.ScheduleStatus?.Next}");
        }
    }
}
