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
using Energinet.DataHub.MessageHub.Client.Peek;
using Energinet.DataHub.MessageHub.Model.Extensions;
using Energinet.DataHub.MessageHub.Model.Model;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.OutgoingMessages.Requesting;

public class SendFailureNotificationHandler : IRequestHandler<SendFailureNotification>
{
    private readonly IDataBundleResponseSender _dataBundleResponseSender;
    private readonly ILogger<SendFailureNotificationHandler> _logger;

    public SendFailureNotificationHandler(IDataBundleResponseSender dataBundleResponseSender, ILogger<SendFailureNotificationHandler> logger)
    {
        _dataBundleResponseSender = dataBundleResponseSender;
        _logger = logger;
    }

    public async Task<Unit> Handle(SendFailureNotification request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        _logger?.LogError(request.FailureDescription);

        var bundleRequest = MessageHubModelFactory.CreateDataBundleRequest(
            request.RequestId,
            request.ReferenceId,
            request.IdempotencyId,
            request.MessageType,
            request.RequestedFormat);

        var error = new DataBundleResponseErrorDto(
            Enum.Parse<DataBundleResponseErrorReason>(request.Reason),
            request.FailureDescription);

        await _dataBundleResponseSender.SendAsync(bundleRequest.CreateErrorResponse(error)).ConfigureAwait(false);
        return Unit.Value;
    }
}
