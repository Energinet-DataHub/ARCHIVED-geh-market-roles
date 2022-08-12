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

using Messaging.Domain.SeedWork;
using Messaging.Domain.Transactions.MoveIn.Events;
using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn
{
    public class MoveInTransaction : Entity
    {
        private State _state = State.Started;
        private bool _hasForwardedMeteringPointMasterData;
        private bool _hasBusinessProcessCompleted;
        private bool _businessProcessIsAccepted;

        public MoveInTransaction(string transactionId, string marketEvaluationPointId, Instant effectiveDate, string? currentEnergySupplierId, string startedByMessageId, string newEnergySupplierId, string? consumerId, string? consumerName, string? consumerIdType)
        {
            TransactionId = transactionId;
            MarketEvaluationPointId = marketEvaluationPointId;
            EffectiveDate = effectiveDate;
            CurrentEnergySupplierId = currentEnergySupplierId;
            StartedByMessageId = startedByMessageId;
            NewEnergySupplierId = newEnergySupplierId;
            ConsumerId = consumerId;
            ConsumerName = consumerName;
            ConsumerIdType = consumerIdType;
            AddDomainEvent(new MoveInWasStarted(TransactionId));
        }

        public enum State
        {
            Started,
            Completed,
        }

        public string TransactionId { get; }

        public string? ProcessId { get; private set; }

        public string MarketEvaluationPointId { get; }

        public Instant EffectiveDate { get; }

        public string? CurrentEnergySupplierId { get; }

        public string StartedByMessageId { get; }

        public string NewEnergySupplierId { get; }

        public string? ConsumerId { get; }

        public string? ConsumerName { get; }

        public string? ConsumerIdType { get; }

        public void BusinessProcessCompleted()
        {
            if (_businessProcessIsAccepted == false)
            {
                throw new MoveInException(
                    "Business process can not be set to completed, when it has not been accepted.");
            }

            _hasBusinessProcessCompleted = true;
            AddDomainEvent(new BusinessProcessWasCompleted(TransactionId));

            if (CurrentEnergySupplierId is not null)
            {
                AddDomainEvent(new EndOfSupplyNotificationChangedToPending());
            }

            CompleteTransactionIfPossible();
        }

        public void AcceptedByBusinessProcess(string processId, string marketEvaluationPointNumber)
        {
            if (_state != State.Started)
            {
                throw new MoveInException($"Cannot accept transaction while in state '{_state.ToString()}'");
            }

            _businessProcessIsAccepted = true;
            ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
            AddDomainEvent(new MoveInWasAccepted(ProcessId, marketEvaluationPointNumber, TransactionId));
        }

        public void RejectedByBusinessProcess()
        {
            EnsureNotCompleted();

            AddDomainEvent(new MoveInWasRejected(TransactionId));
            Complete();
        }

        public void HasForwardedMeteringPointMasterData()
        {
            _hasForwardedMeteringPointMasterData = true;
            CompleteTransactionIfPossible();
        }

        private void CompleteTransactionIfPossible()
        {
            if (_hasBusinessProcessCompleted && _hasForwardedMeteringPointMasterData)
            {
                Complete();
            }
        }

        private void EnsureNotCompleted()
        {
            if (_state != State.Started)
            {
                throw new MoveInException($"Move in transaction '{TransactionId}' has completed. No further actions can be done.");
            }
        }

        private void Complete()
        {
            EnsureNotCompleted();
            _state = State.Completed;
            AddDomainEvent(new MoveInWasCompleted());
        }
    }
}
