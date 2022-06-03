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
using Energinet.DataHub.MarketRoles.Contracts;
using Google.Protobuf;
using Processing.Infrastructure.Transport.Protobuf;
using ForwardMeteringPointDetails = Processing.Application.ChangeOfSupplier.Processing.MeteringPointDetails.ForwardMeteringPointDetails;

namespace Processing.Infrastructure.InternalCommands.Protobuf.Mappers.ChangeOfSupplier
{
    public class ForwardMeteringPointDetailsOutbound : ProtobufOutboundMapper<ForwardMeteringPointDetails>
    {
        protected override IMessage Convert(ForwardMeteringPointDetails obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return new MarketRolesEnvelope()
            {
                ForwardMeteringPointDetails = new Energinet.DataHub.MarketRoles.Contracts.ForwardMeteringPointDetails()
                {
                    Id = obj.Id.ToString(),
                    Transaction = string.Empty,
                    AccountingPointId = obj.AccountingPointId.ToString(),
                    BusinessProcessId = obj.BusinessProcessId.ToString(),
                },
            };
        }
    }
}
