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

using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Domain.BusinessProcesses
{
    public class BusinessProcessType : EnumerationType
    {
        public static readonly BusinessProcessType ChangeOfSupplier =
            new BusinessProcessType(1, nameof(ChangeOfSupplier), BusinessProcessIntent.Supplier);

        public static readonly BusinessProcessType MoveIn =
            new BusinessProcessType(2, nameof(MoveIn), BusinessProcessIntent.Customer);

        public static readonly BusinessProcessType MoveOut =
            new BusinessProcessType(3, nameof(MoveOut), BusinessProcessIntent.Customer);

        public BusinessProcessType(int id, string name, BusinessProcessIntent intent)
            : base(id, name)
        {
            Intent = intent;
        }

        public BusinessProcessIntent Intent { get; }
    }
}
