#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Reflection;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2 : WebApiController
    {

        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/rennmaus-coder/ninaAPI/, https://bump.sh/christian-photo/doc/advanced-api";
        }

        [Route(HttpVerbs.Get, "/version")]
        public void GetVersion()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpContext.WriteToResponse(new HttpResponse() { Response = Assembly.GetAssembly(typeof(AdvancedAPI)).GetName().Version.ToString() });
        }
    }
}