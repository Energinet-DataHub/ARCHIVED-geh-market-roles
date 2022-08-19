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
using System.Threading.Tasks;

namespace Messaging.Application.OutgoingMessages
{
    /// <summary>
    /// Generates notifications as response to message requests
    /// </summary>
    public interface IMessageRequestNotifications
    {
        /// <summary>
        /// Message was saved successfully at storage location
        /// </summary>
        /// <param name="storedMessageLocation">Location of saved message</param>
        Task SavedMessageSuccessfullyAsync(Uri storedMessageLocation);

        /// <summary>
        /// Requested messages was not found
        /// </summary>
        Task RequestedMessagesWasNotFoundAsync(IReadOnlyList<string> messageIds);
    }
}