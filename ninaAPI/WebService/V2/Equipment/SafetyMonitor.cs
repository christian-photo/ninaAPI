#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        public static void StartSafetyWatchers()
        {
            AdvancedAPI.Controls.SafetyMonitor.Connected += async (_, _) => await WebSocketV2.SendAndAddEvent("SAFETY-CONNECTED");
            AdvancedAPI.Controls.SafetyMonitor.Disconnected += async (_, _) => await WebSocketV2.SendAndAddEvent("SAFETY-DISCONNECTED");
            AdvancedAPI.Controls.SafetyMonitor.IsSafeChanged += async (_, _) => await WebSocketV2.SendAndAddEvent("SAFETY-CHANGED");
        }


        [Route(HttpVerbs.Get, "/equipment/safetymonitor/info")]
        public void SafetyMonitorInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISafetyMonitorMediator safetymonitor = AdvancedAPI.Controls.SafetyMonitor;

                SafetyMonitorInfo info = safetymonitor.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/safetymonitor/connect")]
        public async Task SafetyMonitorConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISafetyMonitorMediator safetymonitor = AdvancedAPI.Controls.SafetyMonitor;

                if (!safetymonitor.GetInfo().Connected)
                {
                    await safetymonitor.Rescan();
                    await safetymonitor.Connect();
                }
                response.Response = "Safetymonitor connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/safetymonitor/disconnect")]
        public async Task SafetyMonitorDisconnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISafetyMonitorMediator safetymonitor = AdvancedAPI.Controls.SafetyMonitor;

                if (safetymonitor.GetInfo().Connected)
                {
                    await safetymonitor.Disconnect();
                }
                response.Response = "Safetymonitor disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
