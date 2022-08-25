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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MeteringPoints.RequestResponse.Requests;
using Google.Protobuf;
using Messaging.Application.Transactions.MoveIn;

namespace Messaging.Infrastructure.Transactions.MoveIn;

public class RequestMeteringPointMasterData : IRequestMeteringPointMasterData
{
    private readonly RequestDispatcher<RequestMasterDataConfiguration> _dispatcher;

    public RequestMeteringPointMasterData(RequestDispatcher<RequestMasterDataConfiguration> dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task RequestMasterDataForAsync(FetchMeteringPointMasterData fetchMeteringPointMasterData)
    {
        if (fetchMeteringPointMasterData == null) throw new ArgumentNullException(nameof(fetchMeteringPointMasterData));
        var message = CreateFrom(fetchMeteringPointMasterData);
        await _dispatcher.SendAsync(message).ConfigureAwait(false);
    }

    private static ServiceBusMessage CreateFrom(FetchMeteringPointMasterData fetchMeteringPointMasterData)
    {
        var message = new MasterDataRequest
        {
            GsrnNumber = fetchMeteringPointMasterData.MarketEvaluationPointNumber,
        };
        var bytes = message.ToByteArray();
        ServiceBusMessage serviceBusMessage = new(bytes)
        {
            ContentType = "application/octet-stream;charset=utf-8",
        };
        serviceBusMessage.ApplicationProperties.Add("BusinessProcessId", fetchMeteringPointMasterData.BusinessProcessId);
        serviceBusMessage.ApplicationProperties.Add("TransactionId", fetchMeteringPointMasterData.TransactionId);

        return serviceBusMessage;
    }
}
