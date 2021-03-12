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
using Dapper.NodaTime;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Application.Outbox;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.EntryPoint;
using Energinet.DataHub.MarketData.Infrastructure;
using Energinet.DataHub.MarketData.Infrastructure.ActorMessages;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.EnergySuppliers;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.MarketEvaluationPoints;
using Energinet.DataHub.MarketData.Infrastructure.IntegrationEvents;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Energinet.DataHub.MarketData.Infrastructure.UseCaseProcessing;
using GreenEnergyHub.Json;
using GreenEnergyHub.Messaging;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Energinet.DataHub.MarketData.EntryPoint
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddGreenEnergyHub(typeof(RequestChangeOfSupplier).Assembly, typeof(RequestChangeSupplierCommandHandler).Assembly);
            builder.Services.AddSingleton<IJsonSerializer, JsonSerializer>();
            builder.Services.AddScoped<IHubRehydrator, JsonMessageDeserializer>();

            builder.Services.AddScoped<IDbConnectionFactory>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var connectionString = configuration.GetValue<string>("MARKET_DATA_DB_CONNECTION_STRING");
                return new SqlDbConnectionFactory(connectionString);
            });

            DapperNodaTimeSetup.Register();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProvider>();
            builder.Services.AddScoped<IEventPublisher, EventPublisher>();
            builder.Services.AddScoped<IActorMessagePublisher, ActorMessagePublisher>();
            builder.Services.AddScoped<IMeteringPointRepository, MeteringPointRepository>();
            builder.Services.AddScoped<IEnergySupplierRepository, EnergySupplierRepository>();
            builder.Services.AddScoped<IForwardMessageRepository, ForwardMessageRepository>();
            builder.Services.AddScoped<ForwardMessageService>();

            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkHandlerBehavior<,>));
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PublishIntegrationEventsHandlerBehavior<,>));
            builder.Services.AddScoped<IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>, PublishActorMessageHandlerBehavior>();
        }
    }
}
