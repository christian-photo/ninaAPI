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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        public static void StartFilterWheelWatchers()
        {
            AdvancedAPI.Controls.FilterWheel.Connected += async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-CONNECTED");
            AdvancedAPI.Controls.FilterWheel.Disconnected += async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-DISCONNECTED");
            AdvancedAPI.Controls.FilterWheel.FilterChanged += async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-CHANGED");
        }


        [Route(HttpVerbs.Get, "/equipment/filterwheel/info")]
        public void FilterWheelInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator FilterWheel = AdvancedAPI.Controls.FilterWheel;

                FilterWheelInfo info = FilterWheel.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/connect")]
        public async Task FilterWheelConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    await filterwheel.Connect();
                }
                response.Response = "Filterwheel connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/disconnect")]
        public async Task FilterWheelDisconnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                }
                response.Response = "Filterwheel disconnected";
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
