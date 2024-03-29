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
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Xunit;

namespace Messaging.Tests.Domain.OutgoingMessages;

public class MessageRecordsTests
{
    [Fact]
    public void Must_contain_at_least_one_message_record()
    {
        Assert.Throws<BundleException>(() => MessageRecords.Create(new List<EnqueuedMessage>()));
    }

    [Fact]
    public void All_message_records_must_originate_from_the_same_type_of_process()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage("E65"),
            CreateEnqueuedMessage("E66"),
        };

        Assert.Throws<ProcessTypesDoesNotMatchException>(() =>
            MessageRecords.Create(messages));
    }

    [Fact]
    public void All_message_records_must_have_same_receiver_number()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(receiverNumber: "1234567890098"),
        };

        Assert.Throws<ReceiverIdsDoesNotMatchException>(() =>
            MessageRecords.Create(messages));
    }

    [Fact]
    public void All_message_records_must_have_same_receiver_role()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(receiverRole: "invalid_role"),
        };

        Assert.Throws<ReceiverRoleDoesNotMatchException>(() =>
            MessageRecords.Create(messages));
    }

    [Fact]
    public void All_message_records_must_have_same_sender_number()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(senderNumber: "1234567890098"),
        };

        Assert.Throws<SenderNumberDoesNotMatchException>(() =>
            MessageRecords.Create(messages));
    }

    [Fact]
    public void All_message_records_must_have_same_sender_role()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(senderRole: "invalid_role"),
        };

        Assert.Throws<SenderRoleDoesNotMatchException>(() =>
            MessageRecords.Create(messages));
    }

    [Fact]
    public void All_message_records_must_be_of_same_type()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(messageType: "anotherType"),
        };

        Assert.Throws<MessageTypeDoesNotMatchException>(() =>
            MessageRecords.Create(messages));
    }

    private static EnqueuedMessage CreateEnqueuedMessage(
        string processType = "123",
        string receiverNumber = "1234567890123",
        string receiverRole = "Role1",
        string senderNumber = "1234567890123",
        string senderRole = "Role2",
        string messageType = "MessageType1")
    {
        return new(
            Guid.NewGuid(),
            receiverNumber,
            receiverRole,
            senderNumber,
            senderRole,
            messageType,
            "FakeCategory",
            processType,
            string.Empty);
    }
}
