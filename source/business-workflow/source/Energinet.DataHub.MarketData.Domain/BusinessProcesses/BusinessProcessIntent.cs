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
using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Domain.BusinessProcesses
{
    public class BusinessProcessIntent : EnumerationType
    {
        public static readonly BusinessProcessIntent Supplier =
            new BusinessProcessIntent(0b00000001, nameof(Supplier));

        public static readonly BusinessProcessIntent Customer =
            new BusinessProcessIntent(0b00000010, nameof(Customer));

        public static readonly BusinessProcessIntent AllIntents =
            new BusinessProcessIntent(0b00000011, nameof(AllIntents));

        public BusinessProcessIntent(int id, string name)
            : base(id, name)
        {
        }
    }
}
