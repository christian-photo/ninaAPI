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
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        private static CancellationTokenSource SwitchToken;

        public static void StartSwitchWatchers()
        {
            AdvancedAPI.Controls.Switch.Connected += async (_, _) => await WebSocketV2.SendAndAddEvent("SWITCH-CONNECTED");
            AdvancedAPI.Controls.Switch.Disconnected += async (_, _) => await WebSocketV2.SendAndAddEvent("SWITCH-DISCONNECTED");
        }


        [Route(HttpVerbs.Get, "/equipment/switch/info")]
        public void SwitchInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISwitchMediator sw = AdvancedAPI.Controls.Switch;

                SwitchInfo info = sw.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/switch/connect")]
        public async Task SwitchConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISwitchMediator sw = AdvancedAPI.Controls.Switch;

                if (!sw.GetInfo().Connected)
                {
                    await sw.Rescan();
                    await sw.Connect();
                }
                response.Response = "Switch connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/switch/disconnect")]
        public async Task SwitchDisconnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISwitchMediator sw = AdvancedAPI.Controls.Switch;

                if (sw.GetInfo().Connected)
                {
                    await sw.Disconnect();
                }
                response.Response = "Switch disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/switch/set")]
        public void SwitchSet([QueryField] short index, [QueryField] double value)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISwitchMediator sw = AdvancedAPI.Controls.Switch;

                if (!sw.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Switch not connected", 409));
                }
                SwitchToken?.Cancel();
                SwitchToken = new CancellationTokenSource();
                sw.SetSwitchValue(index, value, AdvancedAPI.Controls.StatusMediator.GetStatus(), SwitchToken.Token);
                response.Response = "Switch value updated";
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
