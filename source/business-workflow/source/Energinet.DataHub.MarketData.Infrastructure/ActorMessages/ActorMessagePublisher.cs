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
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketData.Infrastructure.ActorMessages
{
    public class ActorMessagePublisher : IActorMessagePublisher, ICanInsertDataModel
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public ActorMessagePublisher(IDbConnectionFactory connectionFactory, ISystemDateTimeProvider systemDateTimeProvider, IUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public Task PublishAsync<TMessage>(TMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageType = message.GetType().Name;
            var payload = JsonSerializer.Serialize(message);

            var outboxMessage = new OutboxMessage(_systemDateTimeProvider.Now(), messageType, payload);
            _unitOfWork.RegisterNew(outboxMessage, this);

            return Task.CompletedTask;
        }

        public async Task PersistCreationOfAsync(IDataModel entity)
        {
            var dataModel = (OutboxMessage)entity;

            if (dataModel is null)
            {
                throw new NullReferenceException(nameof(dataModel));
            }

            var insertStatement = $"INSERT INTO [dbo].[OutgoingActorMessages] (OccurredOn, Type, Data, State) VALUES (@OccurredOn, @Type, @Data, @State)";
            await _connectionFactory.GetOpenConnection().ExecuteAsync(insertStatement, new
            {
                OccurredOn = dataModel.OccurredOn,
                Type = dataModel.Type,
                Data = dataModel.Data,
                State = OutboxState.Pending.Id,
            }).ConfigureAwait(false);
        }
    }
}
