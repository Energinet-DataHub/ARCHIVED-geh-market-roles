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

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Messaging.PerformanceTest.MoveIn.Jwt;

internal class JwtBuilder
{
    public static string BuildToken(string uniqueActorNumber)
    {
        var token = new JwtSecurityToken(
            "https://login.microsoftonline.com/4a7411ea-ac71-4b63-9647-b8bd4c5a20e0/v2.0",
            "c7e5dc5c-2ee0-420c-b5d2-586e7527302c",
            SetupClaims(uniqueActorNumber),
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: SetupCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> SetupClaims(string actorNumber)
    {
        return new List<Claim>
        {
            new("roles", "electricalsupplier"),
            new("test-actornumber", actorNumber),
            new("azp", Guid.NewGuid().ToString()),
        };
    }

    private static SigningCredentials SetupCredentials()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test_not_so_secret_key")) { KeyId = "MyKeyId" };
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }
}
