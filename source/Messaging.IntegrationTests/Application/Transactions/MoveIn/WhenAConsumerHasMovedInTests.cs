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
using System.Threading.Tasks;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Xunit;
using MarketEvaluationPoint = Messaging.Domain.MasterData.MarketEvaluationPoints.MarketEvaluationPoint;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class WhenAConsumerHasMovedInTests : TestBase
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMoveInTransactionRepository _transactionRepository;

    public WhenAConsumerHasMovedInTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _transactionRepository = GetService<IMoveInTransactionRepository>();
    }

    [Fact]
    public async Task An_exception_is_thrown_if_transaction_cannot_be_located()
    {
        var processId = "Not existing";
        var command = new SetConsumerHasMovedIn(processId);

        await Assert.ThrowsAsync<TransactionNotFoundException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
    }

    [Fact]
    public async Task The_current_energy_supplier_is_notified_about_end_of_supply()
    {
        var transaction = await ConsumerHasMovedIn().ConfigureAwait(false);

        AssertThat(transaction.TransactionId, DocumentType.GenericNotification.ToString(), BusinessReasonCode.CustomerMoveInOrMoveOut.Code)
            .HasReceiverId(transaction.CurrentEnergySupplierId!)
            .HasReceiverRole(MarketRoles.EnergySupplier)
            .HasSenderId(DataHubDetails.IdentificationNumber)
            .HasSenderRole(MarketRoles.MeteringPointAdministrator)
            .HasReasonCode(null)
            .WithMarketActivityRecord()
                .HasId()
                .HasValidityStart(transaction.EffectiveDate.ToDateTimeUtc())
                .HasOriginalTransactionId(transaction.TransactionId)
                .HasMarketEvaluationPointId(transaction.MarketEvaluationPointId);
    }

    private async Task<MoveInTransaction> ConsumerHasMovedIn()
    {
        var transaction = await StartMoveInTransaction();
        await InvokeCommandAsync(new SetConsumerHasMovedIn(transaction.ProcessId!)).ConfigureAwait(false);
        return transaction;
    }

    private async Task<MoveInTransaction> StartMoveInTransaction()
    {
        await SetupMasterDataDetailsAsync();
        var transaction = new MoveInTransaction(
            SampleData.TransactionId,
            SampleData.MeteringPointNumber,
            _systemDateTimeProvider.Now(),
            SampleData.CurrentEnergySupplierNumber,
            SampleData.OriginalMessageId,
            SampleData.NewEnergySupplierNumber,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ConsumerIdType);

        transaction.AcceptedByBusinessProcess(BusinessRequestResult.Succeeded(Guid.NewGuid().ToString()).ProcessId!, SampleData.MeteringPointNumber);
        transaction.HasForwardedMeteringPointMasterData();
        _transactionRepository.Add(transaction);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        return transaction;
    }

    private Task SetupMasterDataDetailsAsync()
    {
        GetService<IMarketEvaluationPointRepository>().Add(MarketEvaluationPoint.Create(SampleData.CurrentEnergySupplierNumber, SampleData.MeteringPointNumber));
        return Task.CompletedTask;
    }

    private AssertOutgoingMessage AssertThat(string transactionId, string documentType, string processType)
    {
        return AssertOutgoingMessage.OutgoingMessage(transactionId, documentType, processType, GetService<IDbConnectionFactory>());
    }

    private AssertTransaction AssertTransaction()
    {
        return MoveIn.AssertTransaction
            .Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>());
    }
}
