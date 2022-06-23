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
using MediatR;
using Microsoft.Extensions.Logging;
using Processing.Infrastructure.Configuration.SystemTime;

namespace Processing.Infrastructure.Configuration.EventPublishing
{
    public class PublishEventsOnTimeHasPassed : INotificationHandler<TenSecondsHasPassed>
    {
        private readonly EventDispatcher _eventDispatcher;
        private readonly ILogger<PublishEventsOnTimeHasPassed> _logger;

        public PublishEventsOnTimeHasPassed(EventDispatcher eventDispatcher, ILogger<PublishEventsOnTimeHasPassed> logger)
        {
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        public Task Handle(TenSecondsHasPassed notification, CancellationToken cancellationToken)
        {
            try
            {
                return _eventDispatcher.DispatchAsync();
            }
            #pragma warning disable CA1031 // Exceptions thrown here can be any exception
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process integration events.");
                return Task.CompletedTask;
            }
        }
    }
}
