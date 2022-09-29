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

namespace Messaging.IntegrationTests.Application.Actors;

internal static class SampleData
{
    internal static string ActorId => "91207d78-9a32-4bc6-ad20-bff6c038f634";

    internal static string IdentificationNumber => "5148796574821";

    internal static Guid B2CId => Guid.Parse("9222905B-8B02-4D8B-A2C1-3BD51B1AD8D9");
}
