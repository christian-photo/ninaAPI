#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class SafetyWatcher : INinaWatcher
    {
        private readonly Func<object, EventArgs, Task> SafetyConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("SAFETY-CONNECTED");
        private readonly Func<object, EventArgs, Task> SafetyDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("SAFETY-DISCONNECTED");
        private readonly EventHandler<IsSafeEventArgs> SafetyIsSafeChangedHandler = async (_, e) => await WebSocketV2.SendAndAddEvent(
            "SAFETY-CHANGED",
            new Dictionary<string, object>() { { "IsSafe", e.IsSafe } });

        public void StartWatchers()
        {
            AdvancedAPI.Controls.SafetyMonitor.Connected += SafetyConnectedHandler;
            AdvancedAPI.Controls.SafetyMonitor.Disconnected += SafetyDisconnectedHandler;
            AdvancedAPI.Controls.SafetyMonitor.IsSafeChanged += SafetyIsSafeChangedHandler;
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.SafetyMonitor.Connected -= SafetyConnectedHandler;
            AdvancedAPI.Controls.SafetyMonitor.Disconnected -= SafetyDisconnectedHandler;
            AdvancedAPI.Controls.SafetyMonitor.IsSafeChanged -= SafetyIsSafeChangedHandler;
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/equipment/safetymonitor/info")]
        public void SafetyMonitorInfo()
        {
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
        public async Task SafetyMonitorConnect([QueryField] bool skipRescan)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISafetyMonitorMediator safetymonitor = AdvancedAPI.Controls.SafetyMonitor;

                if (!safetymonitor.GetInfo().Connected)
                {
                    if (!skipRescan)
                    {
                        await safetymonitor.Rescan();
                    }
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

        [Route(HttpVerbs.Get, "/equipment/safetymonitor/search")]
        public async Task SafetyMonitorSearch()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISafetyMonitorMediator safetymonitor = AdvancedAPI.Controls.SafetyMonitor;
                var scanResult = await safetymonitor.Rescan();
                response.Response = scanResult;
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
