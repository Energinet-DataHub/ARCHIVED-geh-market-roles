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
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Infrastructure.Configuration.DataAccess;

namespace Messaging.Infrastructure.OutgoingMessages;

public class OutgoingMessageEnqueuer
{
    private readonly B2BContext _context;

    public OutgoingMessageEnqueuer(B2BContext context)
    {
        _context = context;
    }

    public Task EnqueueAsync(EnqueuedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _context.EnqueuedMessages
            .AddAsync(message)
            .AsTask();
    }
}
