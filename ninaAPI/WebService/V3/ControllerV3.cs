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
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3
{
    public partial class ControllerV3 : WebApiController
    {
        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/christian-photo/ninaAPI/, https://bump.sh/christian-photo/doc/advanced-api, https://bump.sh/christian-photo/doc/advanced-api-websockets";
        }

        [Route(HttpVerbs.Get, "/version")]
        public void GetVersion()
        {
            HttpContext.WriteToResponse(new HttpResponse() { Response = Assembly.GetAssembly(typeof(AdvancedAPI)).GetName().Version.ToString() });
        }

        [Route(HttpVerbs.Get, "/time")]
        public void GetTime()
        {
            HttpContext.WriteToResponse(new HttpResponse() { Response = DateTime.Now });
        }

        [Route(HttpVerbs.Get, "/application-start")]
        public void GetApplicationStart()
        {
            HttpContext.WriteToResponse(new HttpResponse() { Response = NINA.Core.Utility.CoreUtil.ApplicationStartDate });
        }

        [Route(HttpVerbs.Get, "/version/nina")]
        public void GetNINAVersion([QueryField] bool friendly)
        {
            HttpContext.WriteToResponse(new HttpResponse() { Response = friendly ? NINA.Core.Utility.CoreUtil.VersionFriendlyName : NINA.Core.Utility.CoreUtil.Version });
        }
    }
}