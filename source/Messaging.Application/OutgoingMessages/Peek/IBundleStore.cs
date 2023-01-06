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
using System.Threading.Tasks;
using Messaging.Application.OutgoingMessages.Dequeue;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.OutgoingMessages.Peek;

/// <summary>
/// Bundlestore interface
/// </summary>
public interface IBundleStore
{
    /// <summary>
    /// Set bundle
    /// </summary>
    /// <param name="bundleId"></param>
    /// <param name="document"></param>
    /// <param name="messageId"></param>
    /// <param name="messageIdsIncluded"></param>
    /// <returns>void</returns>
    Task<bool> SetBundleForAsync(
        BundleId bundleId,
        Stream document,
        Guid messageId,
        IEnumerable<Guid> messageIdsIncluded);

    /// <summary>
    /// Try to register bundle
    /// </summary>
    /// <param name="bundleId"></param>
    /// <returns>boolean indicating success/failure</returns>
    Task<bool> TryRegisterBundleAsync(
        BundleId bundleId);

    /// <summary>
    /// Unregister bundle
    /// </summary>
    /// <param name="bundleId"></param>
    /// <returns>void</returns>
    Task UnregisterBundleAsync(BundleId bundleId);

    /// <summary>
    /// Dequeue bundle
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns>DequeueResult</returns>
    Task<DequeueResult> DequeueAsync(Guid messageId);

    /// <summary>
    /// Get bundle message Id
    /// </summary>
    /// <param name="bundleId"></param>
    /// <returns>Message Id</returns>
    Task<Guid?> GetBundleMessageIdOfAsync(BundleId bundleId);
}
