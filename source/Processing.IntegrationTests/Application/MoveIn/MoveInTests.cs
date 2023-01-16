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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using EnergySupplying.Contracts.BusinessRequests.MoveIn;
using Newtonsoft.Json;
using Processing.Application.Common;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Processing;
using Processing.Domain.BusinessProcesses.MoveIn.Errors;
using Processing.Domain.Customers;
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
using Customer = EnergySupplying.Contracts.BusinessRequests.MoveIn.Customer;

namespace Processing.IntegrationTests.Application.MoveIn
{
    [IntegrationTest]
    public class MoveInTests : TestBase, IAsyncLifetime
    {
        private AccountingPoint? _accountingPoint;

        public MoveInTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        public Task InitializeAsync()
        {
            _accountingPoint = AccountingPoint.CreateConsumption(AccountingPointId.New(), GsrnNumber.Create(SampleData.GsrnNumber));
            GetService<IAccountingPointRepository>().Add(_accountingPoint);
            var energySupplier = new EnergySupplier(EnergySupplierId.New(), GlnNumber.Create(SampleData.GlnNumber));
            GetService<IEnergySupplierRepository>().Add(energySupplier);
            return GetService<IUnitOfWork>().CommitAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Customer_is_registered()
        {
            await SendRequestAsync(CreateRequest()).ConfigureAwait(false);

            var registration = await GetCustomerRegistrationAsync().ConfigureAwait(false);
            Assert.NotNull(registration);
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
            var request = CreateRequest()
                with
                {
                    EnergySupplierNumber = "1234",
                };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            AssertValidationError<UnknownEnergySupplier>(result, "UnknownEnergySupplier");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Accounting_point_must_exist()
        {
            var request = CreateRequest()
                with
                {
                    AccountingPointNumber = "571234567891234551",
                };

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            Assert.False(result.Success);
            AssertValidationError<UnknownAccountingPoint>(result, "UnknownAccountingPoint");
        }

        [Fact]
        public async Task Request_succeeds()
        {
            var requestAdapter = GetService<JsonMoveInAdapter>();

            var request = new RequestV2(
                AccountingPointNumber: SampleData.GsrnNumber,
                EnergySupplierNumber: SampleData.EnergySupplierId,
                EffectiveDate: SampleData.MoveInDate.ToString(),
                Customer: new Customer(SampleData.ConsumerName, SampleData.CustomerNumber));

            var response = await requestAdapter.ReceiveAsync(SerializeToStream(request));

            var responseBody = await System.Text.Json.JsonSerializer.DeserializeAsync<Response>(response.Content);
            Assert.NotNull(responseBody);
            Assert.NotNull(responseBody?.ProcessId);
        }

        [Fact]
        public async Task Integration_event_is_published_when_move_in_is_effectuated()
        {
            await SendRequestAsync(CreateRequest()).ConfigureAwait(false);
            var consumerRegistration = await GetCustomerRegistrationAsync().ConfigureAwait(false);
            var command = new EffectuateConsumerMoveIn(_accountingPoint!.Id.Value, consumerRegistration?.BusinessProcessId.ToString());

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

        private static MoveInRequest CreateRequest()
        {
            return new MoveInRequest(
                new Processing.Application.MoveIn.Customer(SampleData.ConsumerName, SampleData.CustomerNumber),
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

        private async Task<dynamic?> GetCustomerRegistrationAsync()
        {
            return await GetService<IDbConnectionFactory>()
                .GetOpenConnection()
                .QuerySingleOrDefaultAsync(
                    $"SELECT * FROM [dbo].[ConsumerRegistrations] WHERE AccountingPointId = @AccountingPointId AND CustomerName = @CustomerName AND CustomerNumber = @CustomerNumber",
                    new
                    {
                        AccountingPointId = _accountingPoint?.Id.Value,
                        CustomerNumber = SampleData.CustomerNumber,
                        CustomerName = SampleData.ConsumerName,
                    }).ConfigureAwait(false);
        }
    }
}
