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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Messaging.Api.Configuration.Middleware.Correlation;
using Messaging.Application.Common;
using Messaging.Application.OutgoingMessages;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.Infrastructure.InternalCommands;
using Messaging.IntegrationTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Messaging.IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly ServiceCollection _services;
        private readonly DatabaseFixture _databaseFixture;
        private IServiceProvider _serviceProvider;
        private bool _disposed;

        protected TestBase(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _databaseFixture.CleanupDatabase();

            _services = new ServiceCollection();
            CompositionRoot.Initialize(_services)
                .AddDatabaseConnectionFactory(_databaseFixture.ConnectionString)
                .AddDatabaseContext(_databaseFixture.ConnectionString)
                .AddSystemClock(new SystemDateTimeProviderStub())
                .AddCorrelationContext(_ =>
                {
                    var correlation = new CorrelationContext();
                    correlation.SetId(Guid.NewGuid().ToString());
                    return correlation;
                })
                .AddMessagePublishing(_ => new NewMessageAvailableNotifierSpy())
                .AddMessageStorage(_ => new MessageStorageSpy())
                .AddRequestHandler<TestCommandHandler>()
                .AddHttpClientAdapter(_ => new HttpClientSpy())
                .AddMoveInServices(new MoveInConfiguration(new Uri("http://someuri")))
                .AddMessageParserServices();
            _serviceProvider = _services.BuildServiceProvider();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected T GetService<T>()
            where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true)
            {
                return;
            }

            ((ServiceProvider)_serviceProvider).Dispose();
            _disposed = true;
        }

        protected void RemoveService<TService>()
        {
            _services.AddScoped(sp => new DocumentFactory(new List<DocumentWriter>()));
            _serviceProvider = _services.BuildServiceProvider();
        }

        protected void RegisterService<TService>(TService instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _services.AddSingleton(new DocumentFactory(new List<DocumentWriter>()));
        }

        protected Task InvokeCommandAsync(object command)
        {
            return GetService<IMediator>().Send(command);
        }
    }
}
