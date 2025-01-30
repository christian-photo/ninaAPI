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
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Utility.AutoFocus;
using ninaAPI.Utility;
using ninaAPI.WebService.V2.Equipment;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class FocuserWatcher : INinaWatcher, IFocuserConsumer
    {
        private readonly Func<object, EventArgs, Task> FocuserConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FOCUSER-CONNECTED");
        private readonly Func<object, EventArgs, Task> FocuserDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FOCUSER-DISCONNECTED");

        public void Dispose()
        {
            AdvancedAPI.Controls.Focuser.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Focuser.Connected += FocuserConnectedHandler;
            AdvancedAPI.Controls.Focuser.Disconnected += FocuserDisconnectedHandler;
            AdvancedAPI.Controls.Focuser.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Focuser.Connected -= FocuserConnectedHandler;
            AdvancedAPI.Controls.Focuser.Disconnected -= FocuserDisconnectedHandler;
            AdvancedAPI.Controls.Focuser.RemoveConsumer(this);
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo)
        {
            WebSocketV2.SendConsumerEvent("FOCUSER");
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info)
        {
            WebSocketV2.SendAndAddEvent("AUTOFOCUS-FINISHED");
        }

        public void UpdateUserFocused(FocuserInfo info)
        {
            WebSocketV2.SendAndAddEvent("FOCUSER-USER-FOCUSED");
        }
    }

    public partial class ControllerV2
    {
        private static CancellationTokenSource AutoFocusToken;

        [Route(HttpVerbs.Get, "/equipment/focuser/info")]
        public void FocuserInfo()
        {
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
        public async Task FocuserConnect([QueryField] bool skipRescan)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    if (!skipRescan)
                    {
                        await focuser.Rescan();
                    }
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
        public async Task FocuserDisconnect()
        {
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

        // Only works if it was started by the api
        [Route(HttpVerbs.Get, "/equipment/focuser/auto-focus")]
        public void FocuserStartAF([QueryField] bool cancel)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Focuser not connected", 409));
                }
                if (cancel)
                {
                    AutoFocusToken?.Cancel();
                    response.Response = "Autofocus canceled";
                }
                else
                {
                    AutoFocusToken?.Cancel();
                    AutoFocusToken = new CancellationTokenSource();

                    AdvancedAPI.Controls.AutoFocusFactory.Create().StartAutoFocus(
                        AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                        AutoFocusToken.Token,
                        AdvancedAPI.Controls.StatusMediator.GetStatus()
                    );
                    response.Response = "Autofocus started";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/last-af")]
        public void FocuserLastAF()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                string af_folder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "AutoFocus");
                if (Directory.Exists(af_folder))
                {
                    string[] files = Directory.GetFiles(af_folder);
                    if (files.Length > 0)
                    {
                        string newest = files.OrderBy(File.GetCreationTime).Last();
                        response.Response = JsonConvert.DeserializeObject<AutoFocusReport>(File.ReadAllText(newest));
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("No AF available", 500));
                    }
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("No AF available", 500));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/search")]
        public async Task FocuserSearch()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                var scanResult = await focuser.Rescan();
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
