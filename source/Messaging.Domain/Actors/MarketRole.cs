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

using Messaging.Domain.SeedWork;

namespace Messaging.Domain.Actors;

public class MarketRole : EnumerationType
{
    public static readonly MarketRole MeteringPointAdministrator = new(0, "DDZ");
    public static readonly MarketRole EnergySupplier = new(1, "DDQ");
    public static readonly MarketRole GridOperator = new(2, "DDM");
    public static readonly MarketRole MeteringDataAdministrator = new(2, "DGL");

    private MarketRole(int id, string name)
        : base(id, name)
    {
    }

    public override string ToString()
    {
        return Name;
    }
}
