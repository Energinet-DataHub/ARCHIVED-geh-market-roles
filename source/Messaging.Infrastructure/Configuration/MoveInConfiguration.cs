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
using MediatR;
using Messaging.Application.Configuration.TimeEvents;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.Application.Transactions.MoveIn.UpdateCustomer;
using Messaging.Application.Transactions.UpdateCustomer;
using Messaging.Domain.Transactions.MoveIn.Events;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.Infrastructure.Transactions.MoveIn.UpdateCustomer;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration;

internal static class MoveInConfiguration
{
    public static void Configure(
        IServiceCollection services,
        MoveInSettings settings,
        Func<IServiceProvider, IMoveInRequester>? addMoveInRequestService = null,
        Func<IServiceProvider, ICustomerMasterDataClient>? addCustomerMasterDataClient = null,
        Func<IServiceProvider, IMeteringPointMasterDataClient>? addMeteringPointMasterDataClient = null)
    {
        if (addCustomerMasterDataClient is not null)
        {
            services.AddScoped(addCustomerMasterDataClient);
        }
        else
        {
            services.AddScoped<ICustomerMasterDataClient, CustomerMasterDataClient>();
        }

        if (addMoveInRequestService is not null)
        {
            services.AddScoped(addMoveInRequestService);
        }
        else
        {
            services.AddScoped<IMoveInRequester, MoveInRequester>();
        }

        if (addMeteringPointMasterDataClient is not null)
        {
            services.AddScoped(addMeteringPointMasterDataClient);
        }
        else
        {
            services.AddScoped<IMeteringPointMasterDataClient, MeteringPointMasterDataClient>();
        }

        services.AddScoped<MoveInNotifications>();
        services.AddTransient<IRequestHandler<RequestChangeOfSupplierTransaction, Unit>, MoveInRequestHandler>();
        services.AddTransient<IRequestHandler<FetchCustomerMasterData, Unit>, FetchCustomerMasterDataHandler>();
        services.AddTransient<IRequestHandler<FetchMeteringPointMasterData, Unit>, FetchMeteringPointMasterDataHandler>();
        services.AddTransient<IRequestHandler<SetConsumerHasMovedIn, Unit>, SetConsumerHasMovedInHandler>();
        services.AddTransient<IRequestHandler<ForwardMeteringPointMasterData, Unit>, ForwardMeteringPointMasterDataHandler>();
        services.AddTransient<IRequestHandler<NotifyCurrentEnergySupplier, Unit>, NotifyCurrentEnergySupplierHandler>();
        services.AddTransient<IRequestHandler<NotifyGridOperator, Unit>, NotifyGridOperatorHandler>();
        services.AddTransient<IRequestHandler<SendCustomerMasterDataToGridOperator, Unit>, SendCustomerMasterDataToGridOperatorHandler>();
        services.AddTransient<IRequestHandler<SetCurrentKnownCustomerMasterData, Unit>, SetCurrentKnownCustomerMasterDataHandler>();
        services.AddTransient<IRequestHandler<UpdateCustomerMasterData, Unit>, UpdateCustomerMasterDataHandler>();
        services.AddTransient<INotificationHandler<MoveInWasAccepted>, FetchMeteringPointMasterDataWhenAccepted>();
        services.AddTransient<INotificationHandler<MoveInWasAccepted>, FetchCustomerMasterDataWhenAccepted>();
        services.AddTransient<INotificationHandler<EndOfSupplyNotificationChangedToPending>, NotifyCurrentEnergySupplierWhenConsumerHasMovedIn>();
        services.AddTransient<INotificationHandler<BusinessProcessWasCompleted>, NotifyGridOperatorWhenConsumerHasMovedIn>();
        services.AddTransient<INotificationHandler<ADayHasPassed>, DispatchCustomerMasterDataForGridOperatorWhenGracePeriodHasExpired>();
        services.AddSingleton<IUpdateCustomerMasterDataRequestClient, UpdateCustomerMasterDataRequestClient>();
        services.AddSingleton(settings);
    }
}
