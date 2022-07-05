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
using NodaTime;
using Processing.Application.Common.Commands;
using Processing.Infrastructure.Configuration.Serialization;

namespace Processing.Infrastructure.Configuration.InternalCommands
{
    public class QueuedInternalCommand
    {
        public QueuedInternalCommand(Guid id, string type, string data, Instant creationDate)
        {
            Id = id;
            Type = type;
            Data = data;
            CreationDate = creationDate;
        }

        public Guid Id { get; }

        public string Type { get; }

        public string Data { get; }

        public Instant CreationDate { get; private set; }

        public Instant? ProcessedDate { get; set; }

        public void SetProcessed(Instant now)
        {
            ProcessedDate = now;
        }
    }
}
