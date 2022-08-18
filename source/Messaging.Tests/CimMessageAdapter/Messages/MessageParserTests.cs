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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.CimMessageAdapter.Errors;
using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.OutgoingMessages;
using Xunit;

namespace Messaging.Tests.CimMessageAdapter.Messages;

public class MessageParserTests
{
    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser[]
            {
                new JsonMessageParser(),
                new XmlMessageParser(),
            });
    }

    public static IEnumerable<object[]> CreateMessages()
    {
        return new List<object[]>
        {
            new object[] { CimFormat.Xml, CreateXmlMessage() },
            new object[] { CimFormat.Json, CreateJsonMessage() },
        };
    }

    public static IEnumerable<object[]> CreateMessagesWithInvalidStructure()
    {
        return new List<object[]>
        {
            new object[] { CimFormat.Json, CreateInvalidJsonMessage() },
            new object[] { CimFormat.Xml, CreateInvalidXmlMessage() },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessages))]
    public async Task Can_parse_message(CimFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format).ConfigureAwait(false);

        Assert.True(result.Success);
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithInvalidStructure))]
    public async Task Return_error_when_structure_is_invalid(CimFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format).ConfigureAwait(false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error is InvalidMessageStructure);
    }

    [Fact]
    public async Task Throw_if_message_format_is_not_known()
    {
        var parser = new MessageParser(new List<IMessageParser>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAsync(CreateXmlMessage(), CimFormat.Xml)).ConfigureAwait(false);
    }

    private static Stream CreateXmlMessage()
    {
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}Confirm request Change of Supplier.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static Stream CreateInvalidXmlMessage()
    {
        var messageStream = new MemoryStream();
        using var writer = new StreamWriter(messageStream);
        writer.Write("This is not XML");
        writer.Flush();
        messageStream.Position = 0;
        var returnStream = new MemoryStream();
        messageStream.CopyTo(returnStream);
        return returnStream;
    }

    private static MemoryStream CreateJsonMessage()
    {
        return ReadTextFile(
            $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}json{Path.DirectorySeparatorChar}Request Change of Supplier.json");
    }

    private static MemoryStream CreateInvalidJsonMessage()
    {
        return ReadTextFile($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}json{Path.DirectorySeparatorChar}Invalid Request Change of Supplier.json");
    }

    private static MemoryStream ReadTextFile(string path)
    {
        var jsonDoc = File.ReadAllText(path);
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
