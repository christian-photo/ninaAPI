#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.WebService.GET;
using System;
using System.Collections.Generic;

namespace ninaAPI.WebService
{
    public class Controller : WebApiController
    {
        [Route(HttpVerbs.Any, "/get/{resource}/{action}")]
        public Dictionary<string, string> GetInformation(string resource, string action)
        {
            try
            {
                switch (resource.ToLower())
                {
                    case "camera":
                        return EquipmentMediator.GetCamera(action);
                    case "telescope":
                        return EquipmentMediator.GetTelescope(action);
                }
            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Dictionary<string, string>();
        }
    }
}
