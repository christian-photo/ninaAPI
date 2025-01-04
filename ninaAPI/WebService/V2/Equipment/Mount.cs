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
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {

        private static readonly Func<object, EventArgs, Task> MountConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-CONNECTED");
        private static readonly Func<object, EventArgs, Task> MountDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-DISCONNECTED");
        private static readonly Func<object, EventArgs, Task> MountBeforeMeridianFlipHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-BEFORE-FLIP");
        private static readonly Func<object, EventArgs, Task> MountAfterMeridianFlipHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-AFTER-FLIP");
        private static readonly Func<object, EventArgs, Task> MountHomedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-HOMED");
        private static readonly Func<object, EventArgs, Task> MountParkedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-PARKED");
        private static readonly Func<object, EventArgs, Task> MountUnparkedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-UNPARKED");

        public static void StartMountWatchers()
        {
            AdvancedAPI.Controls.Mount.Connected += MountConnectedHandler;
            AdvancedAPI.Controls.Mount.Disconnected += MountDisconnectedHandler;
            AdvancedAPI.Controls.Mount.BeforeMeridianFlip += MountBeforeMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.AfterMeridianFlip += MountAfterMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.Homed += MountHomedHandler;
            AdvancedAPI.Controls.Mount.Parked += MountParkedHandler;
            AdvancedAPI.Controls.Mount.Unparked += MountUnparkedHandler;
        }

        public static void StopMountWatchers()
        {
            AdvancedAPI.Controls.Mount.Connected -= MountConnectedHandler;
            AdvancedAPI.Controls.Mount.Disconnected -= MountDisconnectedHandler;
            AdvancedAPI.Controls.Mount.BeforeMeridianFlip -= MountBeforeMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.AfterMeridianFlip -= MountAfterMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.Homed -= MountHomedHandler;
            AdvancedAPI.Controls.Mount.Parked -= MountParkedHandler;
            AdvancedAPI.Controls.Mount.Unparked -= MountUnparkedHandler;
        }

        [Route(HttpVerbs.Get, "/equipment/mount/info")]
        public void MountInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;
                response.Response = mount.GetInfo();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/connect")]
        public async Task MountConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    await mount.Rescan();
                    await mount.Connect();
                }
                response.Response = "Mount connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/disconnect")]
        public async Task MountDisconnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (mount.GetInfo().Connected)
                {
                    await mount.Disconnect();
                }
                response.Response = "Mount disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/home")]
        public void MountHome()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 409));
                }
                else if (mount.GetInfo().AtHome)
                {
                    response.Response = "Mount already homed";
                }
                else
                {
                    if (mount.GetInfo().Slewing)
                    {
                        mount.StopSlew();
                    }
                    mount.FindHome(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Homing";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/tracking")]
        public void MountTrackingMode([QueryField] int mode)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 409));
                }
                else
                {
                    // Siderial: 0
                    // Lunar: 1
                    // Solar: 2
                    // King: 3
                    // Stopped: 4 (but actually 5)
                    if (mode == 4)
                        mode++;

                    if (mode >= 0 && mode < 6)
                    {
                        response.Response = "Tracking mode changed";
                        response.Success = mount.SetTrackingMode((TrackingMode)mode);
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid tracking mode", 400));
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/park")]
        public void MountPark()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response.Response = "Mount already parked";
                }
                else
                {
                    if (mount.GetInfo().Slewing)
                    {
                        mount.StopSlew();
                    }
                    mount.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Parking";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/unpark")]
        public void MountUnpark()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (!mount.GetInfo().AtPark)
                {
                    response.Response = "Mount not parked";
                }
                else
                {
                    mount.UnparkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Unparking";
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
