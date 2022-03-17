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
using System.Collections;
using System.Collections.Generic;

namespace ninaAPI.WebService
{
    public class Controller : WebApiController
    {

        #region GET
        [Route(HttpVerbs.Any, "/get/history/{id}")]
        public List<Hashtable> GetHistory(string id)
        {
            Logger.Info($"API call: api/get/history/{id}");
            return EquipmentMediator.GetImageHistory(id);
        }

        [Route(HttpVerbs.Any, "/get/profile/{id}")]
        public List<Hashtable> GetProfile(string id)
        {
            Logger.Info($"API call: api/get/profile/{id}");
            return EquipmentMediator.GetProfile(id);
        }


        [Route(HttpVerbs.Any, "/get/{resource}/{action}")]
        public Hashtable GetInformation(string resource, string action)
        {
            Logger.Info($"API call: api/get/{resource}/{action}");
            try
            {
                switch (resource.ToLower())
                {
                    case "camera":
                        return EquipmentMediator.GetCamera(action);
                    case "telescope":
                        return EquipmentMediator.GetTelescope(action);
                    case "focuser":
                        return EquipmentMediator.GetFocuser(action);
                    case "filterwheel":
                        return EquipmentMediator.GetFilterWheel(action);
                    case "guider":
                        return EquipmentMediator.GetGuider(action);
                    case "dome":
                        return EquipmentMediator.GetDome(action);
                    case "rotator":
                        return EquipmentMediator.GetRotator(action);
                    case "safetymonitor":
                        return EquipmentMediator.GetSafetyMonitor(action);
                    case "flatdevice":
                        return EquipmentMediator.GetFlatDevice(action);
                    case "switch":
                        return EquipmentMediator.GetSwitch(action);
                    
                }
            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }
        #endregion

        #region SET

        #endregion
    }
}
