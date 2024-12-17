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
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        public static void StartFocuserWatchers()
        {
            AdvancedAPI.Controls.Focuser.Connected += async (_, _) => await WebSocketV2.SendAndAddEvent("FOCUSER-CONNECTED");
            AdvancedAPI.Controls.Focuser.Disconnected += async (_, _) => await WebSocketV2.SendAndAddEvent("FOCUSER-DISCONNECTED");
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/info")]
        public void FocuserInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                response.Response = focuser.GetInfo();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/connect")]
        public async void FocuserConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    await focuser.Rescan();
                    await focuser.Connect();
                }
                response.Response = "Focuser connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/disconnect")]
        public async void FocuserDisconnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (focuser.GetInfo().Connected)
                {
                    await focuser.Disconnect();
                }
                response.Response = "Focuser disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/move")]
        public void FocuserMove([QueryField] int position)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Focuser not connected", 409));
                }
                focuser.MoveFocuser(position, new CancellationTokenSource().Token);
                response.Response = "Move started";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/auto-focus")]
        public void FocuserStartAF()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Focuser not connected", 409));
                }
                AdvancedAPI.Controls.AutoFocusFactory.Create().StartAutoFocus(
                    AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                    new CancellationTokenSource().Token,
                    AdvancedAPI.Controls.StatusMediator.GetStatus()
                );
                response.Response = "Autofocus started";
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
