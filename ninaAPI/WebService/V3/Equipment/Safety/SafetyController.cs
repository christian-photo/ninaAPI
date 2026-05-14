#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Equipment.Interfaces.Mediator;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Safety
{
    [Route($"/v3/api/equipment/{EquipmentConstants.SafetyMonitorUrlName}")]
    public class SafetyController : Controller
    {
        private readonly ISafetyMonitorMediator safety;

        public SafetyController(ISafetyMonitorMediator safety)
        {
            this.safety = safety;
        }

        [Route("GET", "/")]
        public SafetyInfoResponse SafetyInfo()
        {
            return new SafetyInfoResponse(safety);
        }
    }
}
