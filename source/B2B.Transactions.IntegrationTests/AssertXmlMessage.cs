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
using System.Xml.Linq;
using Xunit;

namespace B2B.Transactions.IntegrationTests
{
    internal static class AssertXmlMessage
    {
        internal static string GetMarketActivityRecordValue(XDocument document, string elementName)
        {
            var header = GetHeaderElement(document);
            var documentNamespace = header?.Name.Namespace!;
            var element = header?.Element(documentNamespace + "MktActivityRecord")?.Element(documentNamespace + elementName);
            return element?.Value ?? string.Empty;
        }

        internal static string? GetMessageHeaderValue(XDocument document, string elementName)
        {
            var header = GetHeaderElement(document);
            return header?.Element(header.Name.Namespace + elementName)?.Value;
        }

        internal static XElement? GetMarketActivityRecordById(XDocument document, string id)
        {
            var marketActivityRecords = document.Root?.Elements()
                .Where(x => x.Name.LocalName.Equals("MktActivityRecord", StringComparison.OrdinalIgnoreCase)).ToList();

            //XNamespace ns = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
            return marketActivityRecords?.Where(x =>
                x.Element(document.Root!.Name.Namespace + "mRID")?.Value.Equals(
                    id,
                    StringComparison.OrdinalIgnoreCase) ?? false).FirstOrDefault();
        }

        internal static void AssertHasHeaderValue(XDocument document, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, GetMessageHeaderValue(document, elementName));
        }

        internal static void AssertMarketActivityRecordValue(XDocument document, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, GetMarketActivityRecordValue(document, elementName));
        }

        internal static void AssertMarketActivityRecordValue(XElement marketActivityRecord, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, GetMarketActivityRecordValue(marketActivityRecord.Document!, elementName));
        }

        private static XElement? GetHeaderElement(XDocument document)
        {
            return document.Root;
        }
    }
}
