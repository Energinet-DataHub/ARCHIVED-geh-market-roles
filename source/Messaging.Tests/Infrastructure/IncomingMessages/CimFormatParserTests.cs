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

using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.SeedWork;
using Messaging.Infrastructure.IncomingMessages;
using Xunit;

namespace Messaging.Tests.Infrastructure.IncomingMessages;

public class CimFormatParserTests
{
    [Theory]
    [InlineData("application/json", nameof(MessageFormat.Json))]
    [InlineData("application/json; charset=utf-8", nameof(MessageFormat.Json))]
    [InlineData("application/xml; charset=utf-8", nameof(MessageFormat.Xml))]
    [InlineData("application/xml", nameof(MessageFormat.Xml))]
    [InlineData("application/xml ", nameof(MessageFormat.Xml))]
    public void Can_parse_from_known_content_header_value(string contentHeaderValue, string expectedCimFormat)
    {
        var expectedFormat = EnumerationType.FromName<MessageFormat>(expectedCimFormat);
        var parsedFormat = CimFormatParser.ParseFromContentTypeHeaderValue(contentHeaderValue);

        Assert.Equal(expectedFormat, parsedFormat);
    }

    [Theory]
    [InlineData("application/text")]
    [InlineData("text")]
    public void Return_null_if_content_type_is_unknown(string contentType)
    {
        var parsedFormat = CimFormatParser.ParseFromContentTypeHeaderValue(contentType);

        Assert.Null(parsedFormat);
    }
}
