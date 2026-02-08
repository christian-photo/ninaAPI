#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using ninaAPI.Utility;
using OxyPlot;
using System;
using System.Collections.Generic;
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

        public async void UpdateDeviceInfo(FocuserInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("FOCUSER");
        }

        public async void UpdateEndAutoFocusRun(AutoFocusInfo info)
        {
            await WebSocketV2.SendAndAddEvent("AUTOFOCUS-FINISHED");
        }

        public async void AutoFocusRunStarting()
        {
            await WebSocketV2.SendAndAddEvent("AUTOFOCUS-STARTING");
        }

        public async void NewAutoFocusPoint(DataPoint dataPoint)
        {
            await WebSocketV2.SendAndAddEvent("AUTOFOCUS-POINT-ADDED", DateTime.Now, new Dictionary<string, object>()
            {
                ["HFR"] = dataPoint.Y,
                ["Position"] = dataPoint.X,
            });
        }

        public async void UpdateUserFocused(FocuserInfo info)
        {
            await WebSocketV2.SendAndAddEvent("FOCUSER-USER-FOCUSED");
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

        private CancellationTokenSource FocuserToken;

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
                else
                {
                    FocuserToken?.Cancel();
                    FocuserToken = new CancellationTokenSource();
                    focuser.MoveFocuser(position, FocuserToken.Token);
                    response.Response = "Move started";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/stop-move")]
        public void FocuserStopMove()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Focuser not connected", 409));
                }
                else
                {
                    FocuserToken?.Cancel();
                    response.Response = "Focuser move stopped";
                }
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

                    var service = AdvancedAPI.Controls.WindowFactory.Create();

                    var autofocus = AdvancedAPI.Controls.AutoFocusFactory.Create();
                    service.Show(autofocus, Loc.Instance["LblAutoFocus"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);

                    autofocus.StartAutoFocus(
                        AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                        AutoFocusToken.Token,
                        AdvancedAPI.Controls.StatusMediator.GetStatus()
                    ).ContinueWith(result =>
                    {
                        AdvancedAPI.Controls.ImageHistory.AppendAutoFocusPoint(result.Result);
                        service.DelayedClose(TimeSpan.FromSeconds(10));
                    });
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
        public async Task FocuserLastAF()
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
                        string json = await Retry.Do(() => File.ReadAllText(newest), TimeSpan.FromMilliseconds(100), 5);
                        response.Response = JsonConvert.DeserializeObject<AutoFocusReport>(json);
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

        [Route(HttpVerbs.Get, "/equipment/focuser/pins/reverse")]
        public async Task FocuserLastAF([QueryField] bool reversing)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                if (AdvancedAPI.Controls.Focuser.GetType().HasMethod("SetReverse"))
                {
                    AdvancedAPI.Controls.Focuser.GetType().GetMethod("SetReverse").Invoke(AdvancedAPI.Controls.Focuser, [reversing]);
                    response.Response = "Reverse set";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Platform not supported", 500));
                }
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
