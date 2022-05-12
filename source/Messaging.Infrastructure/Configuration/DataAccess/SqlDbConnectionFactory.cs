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
using System.Data;
using System.Data.Common;
using Messaging.Application.Configuration.DataAccess;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.Configuration.DataAccess
{
    public class SqlDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateSqlClientConnection()
        {
            // Details on using DbProviderFactories see - https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/obtaining-a-dbproviderfactory
            var factory = DbProviderFactories.GetFactory("Microsoft.Data.SqlClient");
            var connection = factory.CreateConnection();
            if (connection == null) throw new InvalidOperationException("No provider setup for Microsoft.Data.SqlClient");

            connection.ConnectionString = _connectionString;
            return connection;
        }
    }
}
