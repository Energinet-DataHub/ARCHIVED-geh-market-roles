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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace B2B.CimMessageAdapter
{
    public class MessageReceiver
    {
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIds _messageIds;
        private readonly IMarketActivityRecordForwarder _marketActivityRecordForwarder;
        private readonly ITransactionIds _transactionIds;
        private bool _hasInvalidHeaderValues;

        public MessageReceiver(IMessageIds messageIds, IMarketActivityRecordForwarder marketActivityRecordForwarder, ITransactionIds transactionIds)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _marketActivityRecordForwarder = marketActivityRecordForwarder ?? throw new ArgumentNullException(nameof(marketActivityRecordForwarder));
            _transactionIds = transactionIds;
        }

        public async Task<Result> ReceiveAsync(Stream message, string businessProcessType, string version)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var xmlSchema = await GetSchemaAsync(businessProcessType, version).ConfigureAwait(true);
            if (xmlSchema is null)
            {
                return Result.Failure(new ValidationError(
                    $"Schema version {version} for business process type {businessProcessType} does not exist."));
            }

            _hasInvalidHeaderValues = false;
            using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
            {
                try
                {
                    await HandleMessageHeaderValuesAsync(reader).ConfigureAwait(false);
                    await HandleMarketActivityRecordsAsync(reader).ConfigureAwait(false);
                }
                catch (XmlException exception)
                {
                    return Result.Failure(new ValidationError(exception.Message));
                }
            }

            if (_hasInvalidHeaderValues == false)
            {
                await _marketActivityRecordForwarder.CommitAsync().ConfigureAwait(false);
            }

            return _errors.Count == 0 ? Result.Succeeded() : Result.Failure(_errors.ToArray());
        }

        private static Task<XmlSchema?> GetSchemaAsync(string businessProcessType, string version)
        {
            var schemas = new Dictionary<KeyValuePair<string, string>, string>()
            {
                {
                    new KeyValuePair<string, string>("requestchangeofsupplier", "1.0"),
                    "urn-ediel-org-structure-requestchangeofsupplier-0-1.xsd"
                },
            };

            if (schemas.TryGetValue(new KeyValuePair<string, string>(businessProcessType, version), out var schemaName) == false)
            {
                return Task.FromResult(default(XmlSchema));
            }

            return LoadSchemaWithDependentSchemasAsync(schemaName);
        }

        private static async Task<XmlSchema?> LoadSchemaWithDependentSchemasAsync(string location)
        {
            using var reader = new XmlTextReader(location);
            var xmlSchema = XmlSchema.Read(reader, null);
            if (xmlSchema is null)
            {
                throw new XmlSchemaException($"Could not read schema at {location}");
            }

            foreach (XmlSchemaExternal external in xmlSchema.Includes)
            {
                if (external.SchemaLocation == null)
                {
                    continue;
                }

                external.Schema =
                    await LoadSchemaWithDependentSchemasAsync(external.SchemaLocation).ConfigureAwait(false);
            }

            return xmlSchema;
        }

        private static async IAsyncEnumerable<MarketActivityRecord> ExtractFromAsync(XmlReader reader)
        {
            var mrid = string.Empty;
            var marketEvaluationPointmRID = string.Empty;
            var energySupplierMarketParticipantmRID = string.Empty;
            var balanceResponsiblePartyMarketParticipantmRID = string.Empty;
            var customerMarketParticipantmRID = string.Empty;
            var customerMarketParticipantname = string.Empty;
            var startDateAndOrTimedateTime = string.Empty;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType == XmlNodeType.EndElement &&
                    reader.LocalName.Equals("MktActivityRecord", StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                    {
                        continue;
                    }

                    yield return new MarketActivityRecord()
                    {
                        MrId = mrid,
                        CustomerMarketParticipantName = customerMarketParticipantname,
                        CustomerMarketParticipantmRID = customerMarketParticipantmRID,
                        MarketEvaluationPointmRID = marketEvaluationPointmRID,
                        EnergySupplierMarketParticipantmRID = energySupplierMarketParticipantmRID,
                        StartDateAndOrTimeDateTime = startDateAndOrTimedateTime,
                        BalanceResponsiblePartyMarketParticipantmRID =
                            balanceResponsiblePartyMarketParticipantmRID,
                    };
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    TryExtractValueFrom("mRID", reader, (value) => mrid = value);
                    TryExtractValueFrom("marketEvaluationPoint.mRID", reader, (value) => marketEvaluationPointmRID = value);
                    TryExtractValueFrom("marketEvaluationPoint.energySupplier_MarketParticipant.mRID", reader, (value) => energySupplierMarketParticipantmRID = value);
                    TryExtractValueFrom("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID", reader, (value) => balanceResponsiblePartyMarketParticipantmRID = value);
                    TryExtractValueFrom("marketEvaluationPoint.customer_MarketParticipant.mRID", reader, (value) => customerMarketParticipantmRID = value);
                    TryExtractValueFrom("marketEvaluationPoint.customer_MarketParticipant.name", reader, (value) => customerMarketParticipantname = value);
                    TryExtractValueFrom("start_DateAndOrTime.dateTime", reader, (value) => startDateAndOrTimedateTime = value);
                }
            }
        }

        private static void TryExtractValueFrom(string elementName, XmlReader reader, Func<string, string> variable)
        {
            if (reader.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
            {
                variable(reader.ReadElementString());
            }
        }

        private async Task HandleMarketActivityRecordsAsync(XmlReader reader)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType != XmlNodeType.Element || !reader.LocalName.Equals("MktActivityRecord", StringComparison.OrdinalIgnoreCase)) continue;
                await foreach (var marketActivityRecord in ExtractFromAsync(reader))
                {
                    if (await CheckTransactionIdAsync(marketActivityRecord.MrId).ConfigureAwait(false) == false)
                    {
                        _errors.Add(new ValidationError(
                            $"Transaction id '{marketActivityRecord.MrId}' is not unique and will not be processed."));
                    }
                    else
                    {
                        await StoreActivityRecordAsync(marketActivityRecord).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task HandleMessageHeaderValuesAsync(XmlReader reader)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.LocalName.Equals("RequestChangeOfSupplier_MarketDocument", StringComparison.OrdinalIgnoreCase))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.LocalName.Equals("mRID", StringComparison.OrdinalIgnoreCase))
                        {
                            var messageId = reader.ReadElementString();
                            var messageIdIsUnique = await CheckMessageIdAsync(messageId).ConfigureAwait(false);
                            if (messageIdIsUnique == false)
                            {
                                _errors.Add(new DuplicateId($"Message id '{messageId}' is not unique"));
                                _hasInvalidHeaderValues = true;
                            }

                            break;
                        }
                    }

                    break;
                }
            }
        }

        private Task<bool> CheckTransactionIdAsync(string transactionId)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));
            return _transactionIds.TryStoreAsync(transactionId);
        }

        private Task StoreActivityRecordAsync(MarketActivityRecord marketActivityRecord)
        {
            return _marketActivityRecordForwarder.AddAsync(marketActivityRecord);
        }

        private Task<bool> CheckMessageIdAsync(string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            return _messageIds.TryStoreAsync(messageId);
        }

        private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
            };

            settings.Schemas.Add(xmlSchema);
            settings.ValidationEventHandler += OnValidationError;
            return settings;
        }

        private void OnValidationError(object? sender, ValidationEventArgs arguments)
        {
            var message =
                $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
            _errors.Add(new ValidationError(message));
        }
    }
}
