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
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Cancel;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.ActorMessages;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.EnergySuppliers;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.MarketEvaluationPoints;
using Energinet.DataHub.MarketData.Infrastructure.IntegrationEvents;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Energinet.DataHub.MarketData.Infrastructure.UseCaseProcessing;
using Energinet.DataHub.MarketData.IntegrationTests.Application;
using GreenEnergyHub.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketData.IntegrationTests
{
    public class TestBase : IDisposable
    {
        private readonly string _connectionString;

        public TestBase()
        {
            var services = new ServiceCollection();

            _connectionString =
                Environment.GetEnvironmentVariable("MarketData_IntegrationTests_ConnectionString");

            services.AddScoped<IDbConnectionFactory>(sp => new SqlDbConnectionFactory(_connectionString));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProviderStub>();
            services.AddScoped<IEventPublisher, EventPublisherStub>();
            services.AddScoped<IActorMessagePublisher, ActorMessagePublisher>();
            services.AddScoped<IMeteringPointRepository, MeteringPointRepository>();
            services.AddScoped<IEnergySupplierRepository, EnergySupplierRepository>();

            services.AddMediatR(new[]
            {
                typeof(RequestChangeSupplierCommandHandler).Assembly,
            });

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkHandlerBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PublishIntegrationEventsHandlerBehavior<,>));
            services.AddScoped<IPipelineBehavior<RequestChangeOfSupplier, BusinessProcessResult>, PublishActorMessageHandlerBehavior>();
            services.AddScoped<IPipelineBehavior<CancelPendingRegistration, BusinessProcessResult>, PublishCancelMessageHandlerBehavior>();
            services.AddScoped(sp => new ProcessContext());

            services.AddGreenEnergyHub(typeof(RequestChangeOfSupplier).Assembly);

            DapperNodaTimeSetup.Register();

            ServiceProvider = services.BuildServiceProvider();
            Mediator = ServiceProvider.GetRequiredService<IMediator>();
            MeteringPointRepository = ServiceProvider.GetRequiredService<IMeteringPointRepository>();
            SupplierRepository = ServiceProvider.GetRequiredService<IEnergySupplierRepository>();
            UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
            ActorMessagePublisher = ServiceProvider.GetRequiredService<IActorMessagePublisher>();
        }

        protected IServiceProvider ServiceProvider { get; }

        protected IMediator Mediator { get; }

        protected IMeteringPointRepository MeteringPointRepository { get; }

        protected IEnergySupplierRepository SupplierRepository { get; }

        protected IUnitOfWork UnitOfWork { get; }

        protected IActorMessagePublisher ActorMessagePublisher { get; }

        public void Dispose()
        {
            CleanupDatabase();
        }

        protected async Task<TMessage> GetLastMessageFromOutboxAsync<TMessage>()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = $"SELECT * FROM [dbo].[OutgoingActorMessages]";
                var outboxmessage = (await connection.QueryAsync(query).ConfigureAwait(false)).FirstOrDefault();

                var serializer = new GreenEnergyHub.Json.JsonSerializer();
                var @event = serializer.Deserialize<TMessage>(outboxmessage.Data);
                return (TMessage)@event;
            }
        }

        private void CleanupDatabase()
        {
            var cleanupStatement = $"DELETE FROM [dbo].[Relationships] " +
                                   $"DELETE FROM [dbo].[MarketParticipants] " +
                                   $"DELETE FROM [dbo].[MarketEvaluationPoints] " +
                                   $"DELETE FROM [dbo].[OutgoingActorMessages]";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(cleanupStatement);
            }
        }
    }
}
