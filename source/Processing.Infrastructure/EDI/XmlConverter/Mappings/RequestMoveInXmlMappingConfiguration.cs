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

using System.Linq;
using Processing.Application.MoveIn;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter.Mappings
{
    public class RequestMoveInXmlMappingConfiguration : XmlMappingConfigurationBase
    {
        public RequestMoveInXmlMappingConfiguration()
        {
            CreateMapping<RequestMoveIn>("MktActivityRecord", mapper => mapper
                .AddProperty(x => x.AccountingPointGsrnNumber, "marketEvaluationPoint.mRID")
                .AddProperty(x => x.MoveInDate, "start_DateAndOrTime.date")
                .AddProperty(x => x.EnergySupplierGlnNumber, "marketEvaluationPoint.energySupplier_MarketParticipant.mRID")
                .AddProperty(x => x.ConsumerName, "marketEvaluationPoint.customer_MarketParticipant.name")
                .AddProperty(x => x.SocialSecurityNumber, EvaluateSocialSecurityNumber, "marketEvaluationPoint.customer_MarketParticipant.mRID")
                .AddProperty(x => x.VATNumber, EvaluateVatNumber, "marketEvaluationPoint.customer_MarketParticipant.mRID")
                .AddProperty(x => x.TransactionId, "mRID"));
        }

        private static string EvaluateSocialSecurityNumber(XmlElementInfo xmlElementInfo)
        {
            var codingScheme = xmlElementInfo.Attributes.SingleOrDefault(x => x.Name == "codingScheme");

            if (codingScheme is null) return xmlElementInfo.SourceValue;

            return codingScheme.Value == "ARR" ? xmlElementInfo.SourceValue : string.Empty;
        }

        private static string EvaluateVatNumber(XmlElementInfo xmlElementInfo)
        {
            var codingScheme = xmlElementInfo.Attributes.SingleOrDefault(x => x.Name == "codingScheme");

            if (codingScheme is null) return xmlElementInfo.SourceValue;

            return codingScheme.Value == "VA" ? xmlElementInfo.SourceValue : string.Empty;
        }
    }
}
