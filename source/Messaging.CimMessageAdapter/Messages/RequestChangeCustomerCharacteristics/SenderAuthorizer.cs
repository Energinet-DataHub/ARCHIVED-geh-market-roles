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
using System.Threading.Tasks;
using Messaging.Application.Configuration.Authentication;
using Messaging.CimMessageAdapter.Errors;

namespace Messaging.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;

public class SenderAuthorizer : ISenderAuthorizer
{
    private const string EnergySupplierRole = "DDQ";
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly List<ValidationError> _validationErrors = new();

    public SenderAuthorizer(IMarketActorAuthenticator marketActorAuthenticator)
    {
        _marketActorAuthenticator = marketActorAuthenticator;
    }

    public Task<Result> AuthorizeAsync(string senderId, string senderRole)
    {
        if (senderId == null) throw new ArgumentNullException(nameof(senderId));
        if (senderRole == null) throw new ArgumentNullException(nameof(senderRole));

        EnsureSenderIdMatches(senderId);
        EnsureSenderRoleMatches(senderRole);
        EnsureCurrentUserHasRequiredRole(senderRole);

        return Task.FromResult(_validationErrors.Count == 0
            ? Result.Succeeded()
            : Result.Failure(_validationErrors.ToArray()));
    }

    private void EnsureSenderIdMatches(string senderId)
    {
        if (_marketActorAuthenticator.CurrentIdentity.Number.Value.Equals(senderId, StringComparison.OrdinalIgnoreCase) == false)
        {
            _validationErrors.Add(new SenderIdDoesNotMatchAuthenticatedUser());
        }
    }

    private void EnsureSenderRoleMatches(string senderRole)
    {
        if (senderRole.Equals(EnergySupplierRole, StringComparison.OrdinalIgnoreCase) == false)
        {
            _validationErrors.Add(new SenderRoleTypeIsNotAuthorized());
        }
    }

    private void EnsureCurrentUserHasRequiredRole(string senderRole)
    {
        if (_marketActorAuthenticator.CurrentIdentity.HasRole(senderRole) == false)
        {
            _validationErrors.Add(new SenderRoleTypeIsNotAuthorized());
        }
    }
}
