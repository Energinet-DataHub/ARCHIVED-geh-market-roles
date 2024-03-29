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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Messaging.Api
{
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Property name should match environment name")]
    public class RuntimeEnvironment
    {
        public static RuntimeEnvironment Default => new();

        public virtual string? DB_CONNECTION_STRING => GetEnvironmentVariable(nameof(DB_CONNECTION_STRING));

        public virtual string? SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND =>
            GetEnvironmentVariable(nameof(SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND));

        public virtual string? INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME => GetEnvironmentVariable(nameof(INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME));

        public virtual string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(REQUEST_RESPONSE_LOGGING_CONNECTION_STRING));

        public virtual string? REQUEST_RESPONSE_LOGGING_CONTAINER_NAME =>
            GetEnvironmentVariable(nameof(REQUEST_RESPONSE_LOGGING_CONTAINER_NAME));

        public virtual string? AZURE_FUNCTIONS_ENVIRONMENT =>
            GetEnvironmentVariable(nameof(AZURE_FUNCTIONS_ENVIRONMENT));

        public string? SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE =>
            GetEnvironmentVariable(nameof(SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE));

        public virtual bool PERFORMANCE_TEST_ENABLED =>
            bool.Parse(GetEnvironmentVariable(nameof(PERFORMANCE_TEST_ENABLED)) ?? "false");

        public int MAX_NUMBER_OF_PAYLOADS_IN_BUNDLE
        {
            get
            {
                var variable = GetEnvironmentVariable(nameof(MAX_NUMBER_OF_PAYLOADS_IN_BUNDLE));
                return string.IsNullOrWhiteSpace(variable) || int.TryParse(variable, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var value) == false
                    ? 100000
                    : value;
            }
        }

        public virtual Uri AGGREGATION_RESULTS_API_URI =>
            new(GetEnvironmentVariable(nameof(AGGREGATION_RESULTS_API_URI))! + AGGREGATION_RESULTS_API_PATH);

        public string? AGGREGATION_RESULTS_API_PATH =>
            GetEnvironmentVariable(nameof(AGGREGATION_RESULTS_API_PATH));

        public virtual bool IsRunningLocally()
        {
            return AZURE_FUNCTIONS_ENVIRONMENT == "Development";
        }

        protected virtual string? GetEnvironmentVariable(string variable)
            => Environment.GetEnvironmentVariable(variable);
    }
}
