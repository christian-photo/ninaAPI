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
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using ninaAPI.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        // [Route(HttpVerbs.Get, "/equipment/{device}/list-devices")]
        // public void ListDevices(string device)
        // {
        //     HttpResponse response = new HttpResponse();

        //     try
        //     {
        //         var vms = GetDeviceVM(device);
        //         IDeviceChooserVM chooser = vms.Item1;

        //         if (chooser != null)
        //         {
        //             response.Response = chooser.Devices;
        //         }
        //         else
        //         {
        //             response = CoreUtility.CreateErrorTable(new Error("Invalid device", 400));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex);
        //         response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
        //     }

        //     HttpContext.WriteToResponse(response);
        // }

        // [Route(HttpVerbs.Get, "/equipment/{device}/connect")]
        // public async Task DeviceConnect(string device, [QueryField] string to)
        // {
        //     HttpResponse response = new HttpResponse();

        //     try
        //     {
        //         var vms = GetDeviceVM(device);
        //         IDeviceChooserVM chooser = vms.Item1;
        //         object handler = vms.Item2;

        //         if (chooser != null)
        //         {
        //             IDevice d = chooser.Devices.First(x => x.Id == to);
        //             if (d != null)
        //             {
        //                 chooser.SelectedDevice = d;
        //                 bool success = await (Task<bool>)handler.GetType().GetMethod("Connect").Invoke(handler, []);
        //                 response.Success = success;
        //                 response.Response = success ? "Connected" : "";
        //                 response.Error = success ? "" : "Failed to connect";
        //                 response.StatusCode = success ? 200 : 400;
        //             }
        //             else
        //             {
        //                 response = CoreUtility.CreateErrorTable(new Error("Invalid Id", 400));
        //             }
        //         }
        //         else
        //         {
        //             response = CoreUtility.CreateErrorTable(new Error("Invalid device", 400));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex);
        //         response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
        //     }

        //     HttpContext.WriteToResponse(response);
        // }

        // [Route(HttpVerbs.Get, "/equipment/{device}/disconnect")]
        // public async Task DeviceDisconnect(string device)
        // {
        //     HttpResponse response = new HttpResponse();

        //     try
        //     {
        //         var vms = GetDeviceVM(device);
        //         object handler = vms.Item2;

        //         if (handler != null)
        //         {
        //             await (Task)handler.GetType().GetMethod("Disconnect").Invoke(handler, []);
        //             response.Response = "Disconnected";
        //         }
        //         else
        //         {
        //             response = CoreUtility.CreateErrorTable(new Error("Invalid device", 400));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex);
        //         response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
        //     }

        //     HttpContext.WriteToResponse(response);
        // }

        // [Route(HttpVerbs.Get, "/equipment/{device}/rescan")]
        // public async Task DeviceRescan(string device)
        // {
        //     HttpResponse response = new HttpResponse();

        //     try
        //     {
        //         var vms = GetDeviceVM(device);
        //         IDeviceChooserVM chooser = vms.Item1;
        //         object handler = vms.Item2;

        //         if (handler != null)
        //         {
        //             var command = (AsyncCommand<bool>)handler.GetType().GetProperty("RescanDevicesCommand", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(handler);
        //             await command.ExecuteAsync(null);
        //             response.Response = chooser.Devices;
        //         }
        //         else
        //         {
        //             response = CoreUtility.CreateErrorTable(new Error("Invalid device", 400));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex);
        //         response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
        //     }

        //     HttpContext.WriteToResponse(response);
        // }

        private (IDeviceChooserVM, object) GetDeviceVM(string device)
        {
            IDeviceChooserVM chooser = null;
            object handler = null;
            switch (device)
            {
                case "camera":
                    var cam = (CameraVM)typeof(CameraMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Camera);
                    chooser = cam.DeviceChooserVM;
                    handler = cam;
                    break;
                case "dome":
                    var dome = (DomeVM)typeof(DomeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Dome);
                    chooser = dome.DeviceChooserVM;
                    handler = dome;
                    break;
                case "filterwheel":
                    var fw = (FilterWheelVM)typeof(FilterWheelMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.FilterWheel);
                    chooser = fw.DeviceChooserVM;
                    handler = fw;
                    break;
                case "flatdevice":
                    var fd = (FlatDeviceVM)typeof(FlatDeviceMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.FlatDevice);
                    chooser = fd.DeviceChooserVM;
                    handler = fd;
                    break;
                case "focuser":
                    var focuser = (FocuserVM)typeof(FocuserMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Focuser);
                    chooser = focuser.DeviceChooserVM;
                    handler = focuser;
                    break;
                case "guider":
                    var guider = (GuiderVM)typeof(GuiderMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Guider);
                    chooser = guider.DeviceChooserVM;
                    handler = guider;
                    break;
                case "mount":
                    var mount = (TelescopeVM)typeof(TelescopeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Mount);
                    chooser = mount.DeviceChooserVM;
                    handler = mount;
                    break;
                case "rotator":
                    var rotator = (RotatorVM)typeof(RotatorMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Rotator);
                    chooser = rotator.DeviceChooserVM;
                    handler = rotator;
                    break;
                case "safetymonitor":
                    var sf = (SafetyMonitorVM)typeof(SafetyMonitorMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.SafetyMonitor);
                    chooser = sf.DeviceChooserVM;
                    handler = sf;
                    break;
                case "switch":
                    var swit = (SwitchVM)typeof(SwitchMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Switch);
                    chooser = swit.DeviceChooserVM;
                    handler = swit;
                    break;
                case "weather":
                    var weather = (WeatherDataVM)typeof(WeatherDataMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Weather);
                    chooser = weather.DeviceChooserVM;
                    handler = weather;
                    break;
            }
            return (chooser, handler);
        }
    }
}
