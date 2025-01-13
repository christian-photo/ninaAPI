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
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class GuideInfo
    {
        public GuideInfo(GuiderInfo inf, GuideStep step)
        {
            Connected = inf.Connected;
            Name = inf.Name;
            DisplayName = inf.DisplayName;
            Description = inf.Description;
            DriverInfo = inf.DriverInfo;
            DriverVersion = inf.DriverVersion;
            DeviceId = inf.DeviceId;
            CanClearCalibration = inf.CanClearCalibration;
            CanSetShiftRate = inf.CanSetShiftRate;
            CanGetLockPosition = inf.CanGetLockPosition;
            SupportedActions = inf.SupportedActions;
            RMSError = inf.RMSError;
            PixelScale = inf.PixelScale;
            LastGuideStep = step;
        }

        public bool Connected { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string DriverInfo { get; set; }
        public string DriverVersion { get; set; }
        public string DeviceId { get; set; }
        public bool CanClearCalibration { get; set; }

        public bool CanSetShiftRate { get; set; }

        public bool CanGetLockPosition { get; set; }

        public IList<string> SupportedActions { get; set; }

        public RMSError RMSError { get; set; }

        public double PixelScale { get; set; }
        public GuideStep LastGuideStep { get; set; }
    }

    public class GuideStep
    {
        public double RADistanceRaw { get; set; }
        public double DECDistanceRaw { get; set; }

        public double RADuration { get; set; }
        public double DECDuration { get; set; }
    }


    public partial class ControllerV2
    {
        private static GuideStep lastGuideStep { get; set; }
        private static CancellationTokenSource GuideToken;

        private static readonly Func<object, EventArgs, Task> GuiderConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-CONNECTED");
        private static readonly Func<object, EventArgs, Task> GuiderDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-DISCONNECTED");
        private static readonly Func<object, EventArgs, Task> GuiderDitherHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-DITHER");
        private static readonly Func<object, EventArgs, Task> GuiderStartHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-START");
        private static readonly Func<object, EventArgs, Task> GuiderStopHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-STOP");
        private static readonly EventHandler<IGuideStep> GuiderGuideEventHandler = (object sender, IGuideStep e) =>
        {
            lastGuideStep = new GuideStep() { DECDistanceRaw = e.DECDistanceRaw, DECDuration = e.DECDuration, RADistanceRaw = e.RADistanceRaw, RADuration = e.RADuration };
        };

        public static void StartGuiderWatchers()
        {
            AdvancedAPI.Controls.Guider.GuideEvent += GuiderGuideEventHandler;
            AdvancedAPI.Controls.Guider.Connected += GuiderConnectedHandler;
            AdvancedAPI.Controls.Guider.Disconnected += GuiderDisconnectedHandler;
            AdvancedAPI.Controls.Guider.AfterDither += GuiderDitherHandler;
            AdvancedAPI.Controls.Guider.GuidingStarted += GuiderStartHandler;
            AdvancedAPI.Controls.Guider.GuidingStopped += GuiderStopHandler;
        }

        public static void StopGuiderWatchers()
        {
            AdvancedAPI.Controls.Guider.GuideEvent -= GuiderGuideEventHandler;
            AdvancedAPI.Controls.Guider.Connected -= GuiderConnectedHandler;
            AdvancedAPI.Controls.Guider.Disconnected -= GuiderDisconnectedHandler;
            AdvancedAPI.Controls.Guider.AfterDither -= GuiderDitherHandler;
            AdvancedAPI.Controls.Guider.GuidingStarted -= GuiderStartHandler;
            AdvancedAPI.Controls.Guider.GuidingStopped -= GuiderStopHandler;
        }


        [Route(HttpVerbs.Get, "/equipment/guider/info")]
        public void GuiderInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                GuideInfo info = new GuideInfo(guider.GetInfo(), lastGuideStep);
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/guider/connect")]
        public async Task GuiderConnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (!guider.GetInfo().Connected)
                {
                    await guider.Rescan();
                    await guider.Connect();
                }
                response.Response = "Guider connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/guider/disconnect")]
        public async Task GuiderDisconnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (guider.GetInfo().Connected)
                {
                    await guider.Disconnect();
                }
                response.Response = "Guider disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/guider/start")]
        public async Task GuiderStart()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (guider.GetInfo().Connected)
                {
                    GuideToken?.Cancel();
                    GuideToken = new CancellationTokenSource();
                    await guider.StartGuiding(false, AdvancedAPI.Controls.StatusMediator.GetStatus(), GuideToken.Token);
                    response.Response = "Guiding started";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Guider not connected", 409));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/guider/stop")]
        public async Task GuiderStop()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (guider.GetInfo().Connected)
                {
                    await guider.StopGuiding(GuideToken.Token);
                    response.Response = "Guiding stopped";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Guider not connected", 409));
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
