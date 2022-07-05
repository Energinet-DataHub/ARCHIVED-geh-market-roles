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
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAP;
using Messaging.Application.SchemaStore;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Xunit;

namespace Messaging.Tests.OutgoingMessages.CharacteristicsOfACustomerAtAnAP
{
    public class CharacteristicsOfACustomerAtAnAPDocumentWriterTests
    {
        private readonly CharacteristicsOfACustomerAtAnAPDocumentWriter _documentWriter;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;
        private ISchemaProvider? _schemaProvider;

        public CharacteristicsOfACustomerAtAnAPDocumentWriterTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProvider();
            _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
            _documentWriter = new CharacteristicsOfACustomerAtAnAPDocumentWriter(_marketActivityRecordParser);
        }

        [Fact]
        public async Task Document_is_valid()
        {
            var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
            var marketActivityRecords = new List<MarketActivityRecord>()
            {
                CreateMarketActivityRecord(),
                CreateMarketActivityRecord(),
            };

            var message = await _documentWriter.WriteAsync(header, marketActivityRecords.Select(record => _marketActivityRecordParser.From(record)).ToList()).ConfigureAwait(false);

            await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
        }

        private static void AssertMarketActivityRecords(List<MarketActivityRecord> marketActivityRecords, XDocument document)
        {
            AssertXmlMessage.AssertMarketActivityRecordCount(document, 2);
            foreach (var activityRecord in marketActivityRecords)
            {
                var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, activityRecord.Id)!;

                Assert.NotNull(marketActivityRecord);
                AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "originalTransactionIDReference_MktActivityRecord.mRID", activityRecord.OriginalTransactionId);
                AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "validityStart_DateAndOrTime.dateTime", activityRecord.ValidityStart.ToString());
            }
        }

        private MarketActivityRecord CreateMarketActivityRecord()
        {
            return new(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                _systemDateTimeProvider.Now(),
                new MarketEvaluationPoint(
                    "579999993331812345",
                    true,
                    _systemDateTimeProvider.Now(),
                    new MrId("Consumer1Id", "ARR"),
                    "Consumer1",
                    new MrId("Consumer2Id", "ARR"),
                    "Consumer2",
                    false,
                    false,
                    _systemDateTimeProvider.Now(),
                    new List<UsagePointLocation>()
                    {
                        new UsagePointLocation(
                            "D01",
                            Guid.NewGuid().ToString(),
                            new MainAddress(
                                new StreetDetail("001", "StreetName", "1", "1", "1"),
                                new TownDetail("001", "TownName", "TownSection", "DK"),
                                "8000",
                                "40"),
                            "MainAddressName",
                            "AttnName",
                            "Phone1Number",
                            "Phone2Number",
                            "SomeEmailAddress",
                            false),
                    }));
        }

        private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
        {
            var document = XDocument.Load(message);
            AssertXmlMessage.AssertHeader(header, document);

            AssertMarketActivityRecords(marketActivityRecords, document);
            await AssertConformsToSchema(message).ConfigureAwait(false);
        }

        private async Task AssertConformsToSchema(Stream message)
        {
            _schemaProvider = SchemaProviderFactory.GetProvider(MediaTypeNames.Application.Xml);
            var schema = await _schemaProvider.GetSchemaAsync<XmlSchema>("characteristicsofacustomeratanap", "0.1")
                .ConfigureAwait(false);
            await AssertXmlMessage.AssertConformsToSchemaAsync(message, schema!).ConfigureAwait(false);
        }
    }
}
