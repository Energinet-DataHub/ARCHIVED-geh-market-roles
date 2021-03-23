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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using GreenEnergyHub.Messaging;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Cancel
{
    public class CancelPendingRegistrationHandler : IRequestHandler<CancelPendingRegistration, BusinessProcessResult>
    {
        private readonly IRuleEngine<CancelPendingRegistration> _ruleEngine;
        private readonly IMeteringPointRepository _meteringPointRepository;

        public CancelPendingRegistrationHandler(IRuleEngine<CancelPendingRegistration> ruleEngine, IMeteringPointRepository meteringPointRepository)
        {
            _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
            _meteringPointRepository = meteringPointRepository ?? throw new ArgumentNullException(nameof(meteringPointRepository));
        }

        public async Task<BusinessProcessResult> Handle(CancelPendingRegistration command, CancellationToken cancellationToken)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var inputValidationResult = await RunInputValidationsAsync(command).ConfigureAwait(false);
            if (!inputValidationResult.Succeeded)
            {
                return inputValidationResult;
            }

            var meteringPoint = await GetMeteringPointAsync(command.MeteringPointId).ConfigureAwait(false);
            if (meteringPoint == null)
            {
                return BusinessProcessResult.Reject("E10");
            }

            var rulesCheckResult = CheckBusinessRules(command, meteringPoint);
            if (!rulesCheckResult.Succeeded)
            {
                return rulesCheckResult;
            }

            // var balanceSupplierIdOfRegistration =
            //     meteringPoint.GetBalanceSupplierIdOf(new ProcessId(command.RegistrationId));
            // if (!balanceSupplierIdOfRegistration.Equals(new GlnNumber(command.BalanceSupplierId)))
            // {
            //     return BusinessProcessResult.Reject("E16");
            // }

            //meteringPoint.CancelSupplierPendingRegistration(command.RegistrationId, new GlnNumber(command.BalanceSupplierId));
            _meteringPointRepository.Save(meteringPoint);

            return BusinessProcessResult.Success();
        }

        private async Task<BusinessProcessResult> RunInputValidationsAsync(CancelPendingRegistration command)
        {
            var result = await _ruleEngine.ValidateAsync(command).ConfigureAwait(false);
            if (result.Success)
            {
                return BusinessProcessResult.Success();
            }

            var errors = result.Select(error => error.RuleNumber).ToList();
            return BusinessProcessResult.Reject(errors);
        }

        private BusinessProcessResult CheckBusinessRules(CancelPendingRegistration command, MeteringPoint meteringPoint)
        {
            var businessOperationValidation =
                meteringPoint.CanCancelSupplierPendingRegistration(command.RegistrationId, new GlnNumber(command.BalanceSupplierId));

            var brokenRuleErrors = businessOperationValidation.Rules
                .Where(rule => rule.IsBroken)
                .Select(rule => rule.Message)
                .ToList();

            return brokenRuleErrors.Count > 0
                ? BusinessProcessResult.Reject(brokenRuleErrors)
                : BusinessProcessResult.Success();
        }

        private Task<MeteringPoint> GetMeteringPointAsync(string gsrnNumber)
        {
            var meteringPointId = GsrnNumber.Create(gsrnNumber);
            var meteringPoint =
                _meteringPointRepository.GetByGsrnNumberAsync(meteringPointId);
            return meteringPoint;
        }
    }
}
