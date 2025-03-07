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
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class GuideInfo
    {
        public GuideInfo(GuiderInfo inf, GuideStep step, string state)
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

            State = state;
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
        public string State { get; set; }
    }

    public class GuideStep
    {
        public double RADistanceRaw { get; set; }
        public double DECDistanceRaw { get; set; }

        public double RADuration { get; set; }
        public double DECDuration { get; set; }
    }

    public class GuiderWatcher : INinaWatcher, IGuiderConsumer
    {
        public static GuideStep lastGuideStep { get; set; }

        private readonly Func<object, EventArgs, Task> GuiderConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-CONNECTED");
        private readonly Func<object, EventArgs, Task> GuiderDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-DISCONNECTED");
        private readonly Func<object, EventArgs, Task> GuiderDitherHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-DITHER");
        private readonly Func<object, EventArgs, Task> GuiderStartHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-START");
        private readonly Func<object, EventArgs, Task> GuiderStopHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("GUIDER-STOP");
        private readonly EventHandler<IGuideStep> GuiderGuideEventHandler = (object sender, IGuideStep e) =>
        {
            lastGuideStep = new GuideStep() { DECDistanceRaw = e.DECDistanceRaw, DECDuration = e.DECDuration, RADistanceRaw = e.RADistanceRaw, RADuration = e.RADuration };
        };

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Guider.GuideEvent += GuiderGuideEventHandler;
            AdvancedAPI.Controls.Guider.Connected += GuiderConnectedHandler;
            AdvancedAPI.Controls.Guider.Disconnected += GuiderDisconnectedHandler;
            AdvancedAPI.Controls.Guider.AfterDither += GuiderDitherHandler;
            AdvancedAPI.Controls.Guider.GuidingStarted += GuiderStartHandler;
            AdvancedAPI.Controls.Guider.GuidingStopped += GuiderStopHandler;
            AdvancedAPI.Controls.Guider.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Guider.GuideEvent -= GuiderGuideEventHandler;
            AdvancedAPI.Controls.Guider.Connected -= GuiderConnectedHandler;
            AdvancedAPI.Controls.Guider.Disconnected -= GuiderDisconnectedHandler;
            AdvancedAPI.Controls.Guider.AfterDither -= GuiderDitherHandler;
            AdvancedAPI.Controls.Guider.GuidingStarted -= GuiderStartHandler;
            AdvancedAPI.Controls.Guider.GuidingStopped -= GuiderStopHandler;
            AdvancedAPI.Controls.Guider.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(GuiderInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("GUIDER");
        }

        public void Dispose()
        {
            AdvancedAPI.Controls.Guider.RemoveConsumer(this);
        }
    }

    public partial class ControllerV2
    {
        private static CancellationTokenSource GuideToken;

        [Route(HttpVerbs.Get, "/equipment/guider/info")]
        public void GuiderInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                IGuider g = (IGuider)guider.GetDevice();

                GuideInfo info = new GuideInfo(guider.GetInfo(), GuiderWatcher.lastGuideStep, g?.State);
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
        public async Task GuiderConnect([QueryField] bool skipRescan)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (!guider.GetInfo().Connected)
                {
                    if (!skipRescan)
                    {
                        await guider.Rescan();
                    }
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
        public async Task GuiderStart([QueryField] bool calibrate)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (guider.GetInfo().Connected)
                {
                    GuideToken?.Cancel();
                    GuideToken = new CancellationTokenSource();
                    await guider.StartGuiding(calibrate, AdvancedAPI.Controls.StatusMediator.GetStatus(), GuideToken.Token);
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

        [Route(HttpVerbs.Get, "/equipment/guider/clear-calibration")]
        public async Task ClearCalibration()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (guider.GetInfo().Connected)
                {
                    response.Success = await guider.ClearCalibration(new CancellationTokenSource().Token);
                    response.Response = "Calibration cleared";
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

        [Route(HttpVerbs.Get, "/equipment/guider/graph")]
        public void GuiderGraph()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                var handlerField = guider.GetType().GetField("handler",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.FlattenHierarchy);

                IGuiderVM gvm = (IGuiderVM)handlerField.GetValue(guider);
                var guiderProperty = gvm.GetType().GetProperty("GuideStepsHistory");

                GuideStepsHistory history = (GuideStepsHistory)guiderProperty.GetValue(gvm);
                response.Response = history;
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
