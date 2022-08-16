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

using System.Collections.Generic;
using Energinet.DataHub.MessageHub.Model.Model;
using MediatR;
using Messaging.Application.Common.Commands;

namespace Messaging.Application.OutgoingMessages;

public class RequestMessages : ICommand<Unit>
{
    public RequestMessages(IEnumerable<string> messageIds, ResponseFormat responseFormat, double responseVersion)
    {
        MessageIds = messageIds;
        ResponseFormat = responseFormat;
        ResponseVersion = responseVersion;
    }

    public IEnumerable<string> MessageIds { get; }

    public ResponseFormat ResponseFormat { get; }

    public double ResponseVersion { get; }
}
