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
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class FlatDeviceWatcher : INinaWatcher, IFlatDeviceConsumer
    {
        private readonly Func<object, EventArgs, Task> FlatDeviceConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-CONNECTED");
        private readonly Func<object, EventArgs, Task> FlatDeviceDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-DISCONNECTED");
        private readonly Func<object, EventArgs, Task> FlatDeviceLightToggledHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-LIGHT-TOGGLED");
        private readonly Func<object, EventArgs, Task> FlatDeviceOpenedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-COVER-OPENED");
        private readonly Func<object, EventArgs, Task> FlatDeviceClosedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-COVER-CLOSED");
        private readonly Func<object, FlatDeviceBrightnessChangedEventArgs, Task> FlatDeviceBrightnessChangedHandler = async (_, e) => await WebSocketV2.SendAndAddEvent(
            "FLAT-BRIGHTNESS-CHANGED",
            new Dictionary<string, object>() { { "Previous", e.From }, { "New", e.To } });

        public void Dispose()
        {
            AdvancedAPI.Controls.FlatDevice.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.FlatDevice.Connected += FlatDeviceConnectedHandler;
            AdvancedAPI.Controls.FlatDevice.Disconnected += FlatDeviceDisconnectedHandler;
            AdvancedAPI.Controls.FlatDevice.LightToggled += FlatDeviceLightToggledHandler;
            AdvancedAPI.Controls.FlatDevice.Opened += FlatDeviceOpenedHandler;
            AdvancedAPI.Controls.FlatDevice.Closed += FlatDeviceClosedHandler;
            AdvancedAPI.Controls.FlatDevice.BrightnessChanged += FlatDeviceBrightnessChangedHandler;
            AdvancedAPI.Controls.FlatDevice.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.FlatDevice.Connected -= FlatDeviceConnectedHandler;
            AdvancedAPI.Controls.FlatDevice.Disconnected -= FlatDeviceDisconnectedHandler;
            AdvancedAPI.Controls.FlatDevice.LightToggled -= FlatDeviceLightToggledHandler;
            AdvancedAPI.Controls.FlatDevice.Opened -= FlatDeviceOpenedHandler;
            AdvancedAPI.Controls.FlatDevice.Closed -= FlatDeviceClosedHandler;
            AdvancedAPI.Controls.FlatDevice.BrightnessChanged -= FlatDeviceBrightnessChangedHandler;
            AdvancedAPI.Controls.FlatDevice.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(FlatDeviceInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("FLATDEVICE");
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/equipment/flatdevice/info")]
        public void FlatDeviceInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                FlatDeviceInfo info = flat.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/flatdevice/set-light")]
        public void FlatDeviceToggle([QueryField] bool on)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                if (flat.GetInfo().Connected)
                {
                    flat.ToggleLight(on, AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Flatdevice light set";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable("Flatdevice not connected", 409);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/flatdevice/set-cover")]
        public void FlatDeviceCover([QueryField] bool closed)
        {
            HttpResponse response = new HttpResponse();
            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                if (flat.GetInfo().Connected)
                {
                    if (closed)
                    {
                        flat.CloseCover(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    }
                    else
                    {
                        flat.OpenCover(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    }
                    response.Response = "Flatdevice cover set";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable("Flatdevice not connected", 409);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/flatdevice/set-brightness")]
        public void FlatDeviceSetLight([QueryField] int brightness)
        {
            HttpResponse response = new HttpResponse();
            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                if (flat.GetInfo().Connected)
                {
                    flat.SetBrightness(brightness, AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Flatdevice brightness set";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable("Flatdevice not connected", 409);
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
