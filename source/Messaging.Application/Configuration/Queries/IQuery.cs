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

using MediatR;

namespace Messaging.Application.Configuration.Queries;

/// <summary>
/// Data query
/// </summary>
#pragma warning disable CA1040 // This marker interface is needed in order to distinguish between commands and queries
public interface IQuery<out TResult> : IRequest<TResult>
{
}
#pragma warning restore