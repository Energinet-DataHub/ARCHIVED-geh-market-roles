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

namespace Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;

public class Address
{
    public Address(StreetDetail street, TownDetail town)
    {
        Street = street;
        Town = town;
    }

    public StreetDetail Street { get; }

    public TownDetail Town { get; }
}

public class StreetDetail
{
    public StreetDetail(string code, string name, string number, string floorIdentification, string suiteNumber)
    {
        Code = code;
        Name = name;
        Number = number;
        FloorIdentification = floorIdentification;
        SuiteNumber = suiteNumber;
    }

    public string Code { get; }

    public string Name { get; }

    public string Number { get; }

    public string FloorIdentification { get; }

    public string SuiteNumber { get; }
}

public class TownDetail
{
    public TownDetail(string code, string name, string section, string country, string postalCode)
    {
        Code = code;
        Name = name;
        Section = section;
        Country = country;
        PostalCode = postalCode;
    }

    public string Code { get; }

    public string Name { get; }

    public string Section { get; }

    public string Country { get; }

    public string PostalCode { get; }
}
