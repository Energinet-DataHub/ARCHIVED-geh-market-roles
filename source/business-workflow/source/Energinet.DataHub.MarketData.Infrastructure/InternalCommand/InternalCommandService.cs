﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GreenEnergyHub.Json;
using MediatR;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandService : IInternalCommandService
    {
        private readonly IInternalCommandRepository _internalCommandRepository;
        private readonly IMediator _mediator;
        private readonly IJsonSerializer _jsonSerializer;

        public InternalCommandService(IInternalCommandRepository internalCommandRepository, IMediator mediator, IJsonSerializer jsonSerializer)
        {
            _internalCommandRepository = internalCommandRepository;
            _mediator = mediator;
            _jsonSerializer = jsonSerializer;
        }

        public async Task ExecuteUnprocessedInternalCommandsAsync()
        {
            var command = await _internalCommandRepository.GetUnprocessedInternalCommandAsync().ConfigureAwait(false);

            if (command?.Type != null)
            {
                Type type = Type.GetType(command.Type + ", Energinet.DataHub.MarketData.Application") ?? throw new Exception();
                var parsedCommand = _jsonSerializer.Deserialize(command.Data!, type);

                await _mediator.Send(parsedCommand, CancellationToken.None);

                await ExecuteUnprocessedInternalCommandsAsync();
            }
        }
    }
}
