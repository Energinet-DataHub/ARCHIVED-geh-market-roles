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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Application.Transactions;

namespace Messaging.Application.IncomingMessages.RequestChangeOfSupplier
{
    public class RequestChangeOfSupplierHandler
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICorrelationContext _correlationContext;
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;

        public RequestChangeOfSupplierHandler(
            ITransactionRepository transactionRepository,
            IOutgoingMessageStore outgoingMessageStore,
            IUnitOfWork unitOfWork,
            ICorrelationContext correlationContext,
            IMarketActivityRecordParser marketActivityRecordParser)
        {
            _transactionRepository = transactionRepository;
            _outgoingMessageStore = outgoingMessageStore;
            _unitOfWork = unitOfWork;
            _correlationContext = correlationContext;
            _marketActivityRecordParser = marketActivityRecordParser;
        }

        public async Task HandleAsync(IncomingMessage incomingMessage)
        {
            if (incomingMessage == null) throw new ArgumentNullException(nameof(incomingMessage));

            var acceptedTransaction = new AcceptedTransaction(incomingMessage.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);

            var businessProcessResult = await InvokeBusinessProcessAsync(incomingMessage).ConfigureAwait(false);
            if (businessProcessResult.Success == false)
            {
                _outgoingMessageStore.Add(RejectMessageFrom(incomingMessage, acceptedTransaction.TransactionId, businessProcessResult));
            }
            else
            {
                _outgoingMessageStore.Add(ConfirmMessageFrom(incomingMessage, acceptedTransaction.TransactionId));
            }

            await _unitOfWork.CommitAsync().ConfigureAwait(false);
        }

        private static Task<BusinessRequestResult> InvokeBusinessProcessAsync(IncomingMessage incomingMessage)
        {
            var businessProcess = new MoveInRequest()
            {
                ConsumerName = incomingMessage.MarketActivityRecord.ConsumerName,
            };

            return businessProcess.InvokeAsync(businessProcess);
        }

        private OutgoingMessage ConfirmMessageFrom(IncomingMessage incomingMessage, string transactionId)
        {
            var messageId = Guid.NewGuid();
            var marketActivityRecord = new OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord(
                messageId.ToString(),
                transactionId,
                incomingMessage.MarketActivityRecord.MarketEvaluationPointId);

            return new OutgoingMessage(
                "ConfirmRequestChangeOfSupplier",
                incomingMessage.Message.SenderId,
                _correlationContext.Id,
                incomingMessage.Id,
                incomingMessage.Message.ProcessType,
                incomingMessage.Message.SenderRole,
                incomingMessage.Message.ReceiverId,
                incomingMessage.Message.ReceiverRole,
                _marketActivityRecordParser.From(marketActivityRecord));
        }

        private OutgoingMessage RejectMessageFrom(IncomingMessage incomingMessage, string transactionId, BusinessRequestResult businessRequestResult)
        {
            var messageId = Guid.NewGuid();
            var marketActivityRecord = new OutgoingMessages.RejectRequestChangeOfSupplier.MarketActivityRecord(
                messageId.ToString(),
                transactionId,
                incomingMessage.MarketActivityRecord.MarketEvaluationPointId,
                businessRequestResult.ValidationErrors.Select(validationError => new Reason(validationError.Message, validationError.Code)));

            return new OutgoingMessage(
                "RejectRequestChangeOfSupplier",
                incomingMessage.Message.SenderId,
                _correlationContext.Id,
                incomingMessage.Id,
                incomingMessage.Message.ProcessType,
                incomingMessage.Message.SenderRole,
                incomingMessage.Message.ReceiverId,
                incomingMessage.Message.ReceiverRole,
                _marketActivityRecordParser.From(marketActivityRecord));
        }
    }

    #pragma warning disable
    public class MoveInRequest
    {
        public Task<BusinessRequestResult> InvokeAsync(MoveInRequest moveInRequest)
        {
            if (string.IsNullOrEmpty(moveInRequest.ConsumerName))
            {
                return Task.FromResult(BusinessRequestResult.Failure(new ZZValidationError("999", "somemessage")));
            }
            return Task.FromResult<BusinessRequestResult>(BusinessRequestResult.Succeeded());
        }

        public string? ConsumerName { get; set; }
    }

    public class BusinessRequestResult
    {
        private BusinessRequestResult()
        {
        }

        private BusinessRequestResult(IReadOnlyCollection<ZZValidationError> validationErrors)
        {
            ValidationErrors = validationErrors;
        }

        public bool Success => ValidationErrors.Count == 0;

        public IReadOnlyCollection<ZZValidationError> ValidationErrors { get; } = new List<ZZValidationError>();

        public static BusinessRequestResult Failure(params ZZValidationError[] validationErrors)
        {
            return new BusinessRequestResult(validationErrors);
        }

        public static BusinessRequestResult Succeeded()
        {
            return new BusinessRequestResult();
        }
    }

    public class ZZValidationError
    {
        public ZZValidationError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; protected init; } = string.Empty;

        public string Message { get; protected init; } = string.Empty;
    }
}
