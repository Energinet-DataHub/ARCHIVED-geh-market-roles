// Copyright 2020 Energinet DataHub A/S
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
using Processing.Application.AccountingPoints;
using Processing.Application.Common.Commands;
using Processing.Application.EnergySuppliers;
using Processing.Application.MoveIn.Processing;
using Processing.Infrastructure.Configuration.InternalCommands;
using SimpleInjector;

namespace Processing.Infrastructure.Configuration
{
    public static class InternalCommandsRegistration
    {
        public static void AddInternalCommandsProcessing(this Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            container.Register<CommandSchedulerFacade>(Lifestyle.Scoped);
            container.Register<ICommandScheduler, CommandScheduler>(Lifestyle.Scoped);
            container.Register<InternalCommandProcessor>(Lifestyle.Scoped);
            container.Register<InternalCommandAccessor>(Lifestyle.Scoped);
            container.Register<CommandExecutor>(Lifestyle.Scoped);
            RegisterCommands(container);
        }

        private static void RegisterCommands(Container container)
        {
            var mapper = new InternalCommandMapper();
            mapper.Add("CreateAccountingPoint", typeof(CreateAccountingPoint));
            mapper.Add("EffectuateConsumerMoveIn", typeof(EffectuateConsumerMoveIn));
            mapper.Add("CreateEnergySupplier", typeof(CreateEnergySupplier));

            container.Register<InternalCommandMapper>(() => mapper, Lifestyle.Singleton);
        }
    }
}
