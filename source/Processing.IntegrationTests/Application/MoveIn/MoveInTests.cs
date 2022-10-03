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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.BusinessRequests.MoveIn;
using Dapper;
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Newtonsoft.Json;
using Processing.Application.Common;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Processing;
using Processing.Domain.BusinessProcesses.MoveIn.Errors;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.EnergySuppliers.Errors;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Errors;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.Configuration.DataAccess;
using Processing.Infrastructure.Configuration.EventPublishing;
using Processing.Infrastructure.Configuration.EventPublishing.Protobuf;
using Processing.Infrastructure.RequestAdapters;
using Processing.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using Customer = Contracts.BusinessRequests.MoveIn.Customer;

namespace Processing.IntegrationTests.Application.MoveIn
{
    [IntegrationTest]
    public class MoveInTests : TestBase
    {
        public MoveInTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Consumer_is_registered()
        {
            var accountingPoint =
                AccountingPoint.CreateConsumption(AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber));
            GetService<IAccountingPointRepository>().Add(accountingPoint);
            var energySupplier = new EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
            GetService<IEnergySupplierRepository>().Add(energySupplier);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);

            await SendRequestAsync(CreateRequest()).ConfigureAwait(false);

            var registered = await GetService<IDbConnectionFactory>()
                .GetOpenConnection()
                .ExecuteScalarAsync<bool>(
                    $"SELECT COUNT(1) FROM [dbo].[ConsumerRegistrations] WHERE AccountingPointId = @AccountingPointId AND CustomerName = @CustomerName AND CustomerNumber = @CustomerNumber",
                    new
                    {
                        AccountingPointId = accountingPoint.Id.Value,
                        CustomerNumber = SampleData.ConsumerSSN,
                        CustomerName = SampleData.ConsumerName,
                    }).ConfigureAwait(false);

            Assert.True(registered);
        }

        [Fact]
        public async Task Accounting_point_gsrn_number_is_required()
        {
            var request = CreateRequest() with
            {
                AccountingPointNumber = string.Empty,
            };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<GsrnNumberIsRequired>(result, "GsrnNumberIsRequired");
        }

        [Fact]
        public async Task Accounting_point_gsrn_number_must_be_valid()
        {
            var request = CreateRequest() with
            {
                AccountingPointNumber = "Not a valid GSRN number",
            };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<InvalidGsrnNumber>(result, "InvalidGsrnNumber");
        }

        [Fact]
        public async Task Customer_number_is_required()
        {
            var request = CreateRequest() with
            {
                Customer = new Processing.Application.MoveIn.Customer("ConsumerName", string.Empty),
            };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<ConsumerIdentifierIsRequired>(result, "ConsumerIdentifierIsRequired");
        }

        [Fact]
        public async Task Customer_number_must_be_valid()
        {
            var request = CreateRequest() with
            {
                Customer = new Processing.Application.MoveIn.Customer("ConsumerName", "Invalid_customer_number"),
            };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<InvalidCustomerNumber>(result);
        }

        [Fact]
        public async Task Customer_name_is_required()
        {
            var request = CreateRequest() with
            {
                Customer = new Processing.Application.MoveIn.Customer(),
            };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<ConsumerNameIsRequired>(result, "ConsumerNameIsRequired");
        }

        [Fact]
        public async Task Energy_supplier_must_be_known()
        {
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<UnknownEnergySupplier>(result, "UnknownEnergySupplier");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Accounting_point_must_exist()
        {
            CreateEnergySupplier(Guid.NewGuid(), SampleData.GlnNumber);
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            Assert.False(result.Success);
            AssertValidationError<UnknownAccountingPoint>(result, "UnknownAccountingPoint");
        }

        [Fact]
        public async Task Request_succeeds()
        {
            var requestAdapter = GetService<JsonMoveInAdapter>();
            CreateEnergySupplier(Guid.NewGuid(), SampleData.GlnNumber);
            CreateAccountingPoint();
            SaveChanges();

            var request = new RequestV2(
                AccountingPointNumber: SampleData.GsrnNumber,
                EnergySupplierNumber: SampleData.EnergySupplierId,
                EffectiveDate: SampleData.MoveInDate.ToString(),
                Customer: new Customer(SampleData.ConsumerName, SampleData.ConsumerSSN));

            var response = await requestAdapter.ReceiveAsync(SerializeToStream(request));

            var responseBody = await System.Text.Json.JsonSerializer.DeserializeAsync<Response>(response.Content);
            Assert.NotNull(responseBody);
            Assert.NotNull(responseBody?.ProcessId);
        }

        [Fact]
        public async Task Move_in_on_top_of_move_in_should_result_in_reject_message()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest(false);
            await SendRequestAsync(request).ConfigureAwait(false);
            await SendRequestAsync(request).ConfigureAwait(false);
        }

        [Fact]
        public async Task Integration_event_is_published_when_move_in_is_effectuated()
        {
            var (accountingPoint, processId) = await SetupScenarioAsync().ConfigureAwait(false);
            var command = new EffectuateConsumerMoveIn(accountingPoint.Id.Value, processId.Value.ToString());

            await InvokeCommandAsync(command).ConfigureAwait(false);

            var consumerMovedInEvent = FindIntegrationEvent<ConsumerMovedIn>();
            Assert.NotNull(consumerMovedInEvent);
            Assert.Equal(command.AccountingPointId.ToString(), consumerMovedInEvent?.AccountingPointId);
            Assert.Equal(command.ProcessId, consumerMovedInEvent?.ProcessId);
        }

        private static void AssertValidationError<TRuleError>(BusinessProcessResult rulesValidationResult, string? expectedErrorCode = null, bool errorExpected = true)
        where TRuleError : ValidationError
        {
            if (rulesValidationResult == null) throw new ArgumentNullException(nameof(rulesValidationResult));
            var error = rulesValidationResult.ValidationErrors.FirstOrDefault(error => error is TRuleError);
            Assert.NotNull(error);
            if (expectedErrorCode is not null)
            {
                Assert.Equal(expectedErrorCode, error?.Code);
            }
        }

        private static MoveInRequest CreateRequest(bool registerConsumerBySSN = true)
        {
            var consumerIdType = registerConsumerBySSN ? ConsumerIdentifierType.CPR : ConsumerIdentifierType.CVR;
            var consumerId = consumerIdType == ConsumerIdentifierType.CPR ? SampleData.ConsumerSSN : SampleData.ConsumerVAT;

            return new MoveInRequest(
                new Processing.Application.MoveIn.Customer(SampleData.ConsumerName, SampleData.ConsumerSSN),
                SampleData.GlnNumber,
                SampleData.GsrnNumber,
                SampleData.MoveInDate);
        }

        private static MemoryStream SerializeToStream(object request)
        {
            var stream = new MemoryStream();
            using var streamWriter = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.Serialize(jsonWriter, request);
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private async Task<(AccountingPoint AccountingPoint, BusinessProcessId ProcessId)> SetupScenarioAsync()
        {
            var accountingPoint = CreateAccountingPoint();
            CreateEnergySupplier(Guid.NewGuid(), SampleData.GlnNumber);
            SaveChanges();

            var requestMoveIn = new MoveInRequest(
                new Processing.Application.MoveIn.Customer(SampleData.ConsumerName, SampleData.ConsumerSSN),
                SampleData.GlnNumber,
                SampleData.GsrnNumber,
                SampleData.MoveInDate);

            var result = await SendRequestAsync(requestMoveIn).ConfigureAwait(false);

            if (result.ProcessId is null)
            {
                throw new InvalidOperationException("Failed to setup scenario.");
            }

            return (accountingPoint, BusinessProcessId.Create(result.ProcessId));
        }

        private TEvent? FindIntegrationEvent<TEvent>()
        {
            var mapper = GetService<IntegrationEventMapper>();
            var eventMetadata = mapper.GetByType(typeof(TEvent));
            var context = GetService<MarketRolesContext>();
            var message = context.OutboxMessages.FirstOrDefault(message => message.Type == eventMetadata.EventName);
            if (message is null)
            {
                return default;
            }

            var parser = GetService<MessageParser>();
            return (TEvent)parser.GetFrom(eventMetadata.EventName, message.Data);
        }
    }
}
