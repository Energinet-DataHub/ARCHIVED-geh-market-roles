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
using System.Linq;
using Messaging.Domain.SeedWork;

namespace Messaging.Application.OutgoingMessages;

public sealed class DocumentNameCode : EnumerationType
{
    public static readonly DocumentNameCode ConfirmationOfStartOfSupply = new(
        0,
        nameof(ConfirmationOfStartOfSupply),
        "414");

    public static readonly DocumentNameCode NotificationToSupplier = new(
        1,
        nameof(NotificationToSupplier),
        "E44");

    public static readonly DocumentNameCode MasterDataMeteringPoint = new(
        2,
        nameof(MasterDataMeteringPoint),
        "E07");

    private DocumentNameCode(int id, string name, string code)
        : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }
}
