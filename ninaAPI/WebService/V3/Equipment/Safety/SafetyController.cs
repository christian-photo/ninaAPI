#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Safety
{
    public class SafetyController : WebApiController
    {
        private readonly ISafetyMonitorMediator safety;
        private readonly ResponseHandler responseHandler;

        public SafetyController(ISafetyMonitorMediator safety, ResponseHandler responseHandler)
        {
            this.safety = safety;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task SafetyInfo()
        {
            await responseHandler.SendObject(HttpContext, safety.GetInfo());
        }
    }
}
