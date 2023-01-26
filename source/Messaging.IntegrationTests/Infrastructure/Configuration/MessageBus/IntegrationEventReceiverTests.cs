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
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.Infrastructure.Configuration.Processing.Inbox;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Configuration.MessageBus;

public class IntegrationEventReceiverTests : TestBase
{
    private readonly IntegrationEventReceiver _receiver;

    public IntegrationEventReceiverTests(DatabaseFixture databaseFixture)
     : base(databaseFixture)
    {
        _receiver = new IntegrationEventReceiver(GetService<B2BContext>(), GetService<ISystemDateTimeProvider>());
    }

    [Fact]
    public async Task Event_is_registered()
    {
        var eventId = "1";

        await EventIsReceived(eventId);

        await EventIsRegisteredWithInbox(eventId);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_already_registered()
    {
        var eventId = "1";
        await EventIsReceived(eventId).ConfigureAwait(false);

        await EventIsReceived(eventId).ConfigureAwait(false);

        await EventIsRegisteredWithInbox(eventId);
    }

    [Fact]
    public async Task Event_is_marked_as_processed_when_a_handler_has_handled_it_successfully()
    {
        var eventId = "1";
        await EventIsReceived(eventId).ConfigureAwait(false);

        await ProcessInboxMessages().ConfigureAwait(false);

        await EventIsMarkedAsProcessed(eventId).ConfigureAwait(false);
    }

    private static byte[] CreateEventPayload(TestIntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }

    private Task EventIsReceived(string eventId)
    {
        var eventType = nameof(TestIntegrationEvent);
        var @event = new TestIntegrationEvent();
        var eventPayload = CreateEventPayload(@event);

        return _receiver.ReceiveAsync(eventId, eventType, eventPayload);
    }

    private async Task EventIsRegisteredWithInbox(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var isRegistered = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM b2b.InboxMessages WHERE Id = @EventId", new { EventId = eventId, });
        Assert.True(isRegistered);
    }

    private async Task EventIsMarkedAsProcessed(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var isProcessed = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM b2b.InboxMessages WHERE Id = @EventId AND ProcessedDate IS NOT NULL", new { EventId = eventId, });
        Assert.True(isProcessed);
    }

    #pragma warning disable
    private Task ProcessInboxMessages()
    {
        var inboxProcessor = new InboxProcessor(
            GetService<IDatabaseConnectionFactory>(),
            GetService<IMediator>(),
            GetService<ISystemDateTimeProvider>());
        return inboxProcessor.ProcessMessagesAsync();
    }
}

#pragma warning disable
public class InboxProcessor
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IMediator _mediator;
    private readonly ISystemDateTimeProvider _dateTimeProvider;

    private readonly List<IIntegrationEventMapper> _mappers = new()
    {
        new TestIntegrationEventMapper(),
    };

    public InboxProcessor(IDatabaseConnectionFactory connectionFactory, IMediator mediator, ISystemDateTimeProvider dateTimeProvider)
    {
        _connectionFactory = connectionFactory;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
    }
    public async Task ProcessMessagesAsync()
    {
        var messages = await FindPendingMessages();

        foreach (var message in messages)
        {
            var notification = MapperFor(message.EventType).MapFrom(message.EventPayload);
            await _mediator.Publish(notification).ConfigureAwait(false);

            await MarkAsProcessedAsync(message).ConfigureAwait(false);
        }
    }

    private async Task MarkAsProcessedAsync(InboxMessage message)
    {
        var updateStatement = $"UPDATE b2b.InboxMessages " +
                              "SET ProcessedDate = @Now " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        await connection.ExecuteAsync(updateStatement, new
        {
            Id = message.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
        }).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<InboxMessage>> FindPendingMessages()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var sql = "SELECT " +
                  $"Id AS {nameof(InboxMessage.Id)}, " +
                  $"EventType AS {nameof(InboxMessage.EventType)}, " +
                  $"EventPayload AS {nameof(InboxMessage.EventPayload)} " +
                  "FROM b2b.InboxMessages " +
                  "WHERE ProcessedDate IS NULL " +
                  "ORDER BY OccurredOn";

        var messages = await connection.QueryAsync<InboxMessage>(sql).ConfigureAwait(false);
        return messages.ToList();
    }

    private IIntegrationEventMapper MapperFor(string eventType)
    {
        return _mappers.First(mapper => mapper.CanHandle(eventType));
    }
}

public class TestIntegrationEventMapper : IIntegrationEventMapper
{
    public INotification MapFrom(byte[] payload)
    {
        var integrationEvent = JsonSerializer.Deserialize<TestIntegrationEvent>(payload);
        return new TestNotification(integrationEvent.Property1);
    }

    public bool CanHandle(string eventType)
    {
        return eventType.Equals(nameof(TestIntegrationEvent));
    }
}

public interface IIntegrationEventMapper
{
    INotification MapFrom(byte[] payload);
    bool CanHandle(string eventType);
}

public class TestIntegrationEvent
{
    public string Property1 { get; set; } = "Test";
}

public class TestNotification : INotification
{
    public TestNotification(string aProperty)
    {
        AProperty = aProperty;
    }

    public string AProperty { get; }
}

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
