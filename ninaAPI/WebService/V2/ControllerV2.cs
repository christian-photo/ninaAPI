#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ninaAPI.Utility;
using SimpleW;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2 : Controller
    {
        private readonly ServiceProvider provider;

        public ControllerV2(ServiceProvider provider)
        {
            this.provider = provider;
        }

        [Route("GET", "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/rennmaus-coder/ninaAPI/, https://christian-photo.github.io/github-page/projects/ninaAPI/v2/doc/api, https://github.com/christian-photo/ninaAPI/wiki/Websocket-V2";
        }

        [Route("GET", "/version")]
        public void GetVersion()
        {
            Response.WriteToResponse(new CustomResponse() { Response = Assembly.GetAssembly(typeof(AdvancedAPI)).GetName().Version.ToString() });
        }

        [Route("GET", "/time")]
        public void GetTime()
        {
            Response.WriteToResponse(new CustomResponse() { Response = DateTime.Now });
        }

        [Route("GET", "/application-start")]
        public void GetApplicationStart()
        {
            Response.WriteToResponse(new CustomResponse() { Response = NINA.Core.Utility.CoreUtil.ApplicationStartDate });
        }

        [Route("GET", "/version/nina")]
        public void GetNINAVersion(bool friendly = false)
        {
            Response.WriteToResponse(new CustomResponse() { Response = friendly ? NINA.Core.Utility.CoreUtil.VersionFriendlyName : NINA.Core.Utility.CoreUtil.Version });
        }
    }
}