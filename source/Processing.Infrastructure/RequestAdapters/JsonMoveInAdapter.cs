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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergySupplying.Contracts.BusinessRequests.MoveIn;
using MediatR;
using Processing.Application.Common;
using Processing.Application.MoveIn;
using Processing.Infrastructure.Configuration.Serialization;
using Customer = Processing.Application.MoveIn.Customer;

namespace Processing.Infrastructure.RequestAdapters
{
    public class JsonMoveInAdapter
    {
        private readonly IJsonSerializer _serializer;
        private readonly IMediator _mediator;

        public JsonMoveInAdapter(IJsonSerializer serializer, IMediator mediator)
        {
            _serializer = serializer;
            _mediator = mediator;
        }

        public async Task<Result> ReceiveAsync(Stream request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var requestDto = await ExtractRequestFromAsync(request).ConfigureAwait(false);
            var command = MapToCommandFrom(requestDto);

            var businessProcessResult = await _mediator.Send(command).ConfigureAwait(false);

            return CreateResult(businessProcessResult);
        }

        private static MoveInRequest MapToCommandFrom(RequestV2 request)
        {
            var command = new MoveInRequest(
                new Customer(
                    request.Customer?.Name!, request.Customer?.Number!),
                request.EnergySupplierNumber!,
                request.AccountingPointNumber!,
                request.EffectiveDate!);
            return command;
        }

        private static async Task<string> ExtractJsonFromAsync(Stream request)
        {
            using var streamReader = new StreamReader(request);
            return await streamReader.ReadToEndAsync().ConfigureAwait(false);
        }

        private async Task<RequestV2> ExtractRequestFromAsync(Stream request)
        {
            var json = await ExtractJsonFromAsync(request).ConfigureAwait(false);
            var requestDto = DeserializeToRequest(json);
            return requestDto;
        }

        private RequestV2 DeserializeToRequest(string json)
        {
            var requestDto = _serializer.Deserialize<RequestV2>(json);
            return requestDto;
        }

        private Result CreateResult(BusinessProcessResult businessProcessResult)
        {
            var response = new Response(businessProcessResult.ValidationErrors.Select(error => error.Code).ToList(), businessProcessResult.ProcessId);
            var content = new MemoryStream(Encoding.UTF8.GetBytes(_serializer.Serialize(response)));
            return new Result(content);
        }
    }
}
