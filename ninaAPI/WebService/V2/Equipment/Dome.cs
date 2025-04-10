#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Com.DriverAccess;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{

    public class DomeWatcher : INinaWatcher, IDomeConsumer
    {
        private readonly Func<object, EventArgs, Task> DomeConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-CONNECTED");
        private readonly Func<object, EventArgs, Task> DomeDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-DISCONNECTED");
        private readonly Func<object, EventArgs, Task> DomeClosedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-SHUTTER-CLOSED");
        private readonly Func<object, EventArgs, Task> DomeOpenedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-SHUTTER-OPENED");
        private readonly Func<object, EventArgs, Task> DomeHomedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-HOMED");
        private readonly Func<object, EventArgs, Task> DomeParkedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-PARKED");
        private readonly Func<object, DomeEventArgs, Task> DomeSlewedHandler = async (_, e) => await WebSocketV2.SendAndAddEvent("DOME-SLEWED", new Dictionary<string, object>() {
            { "From", e.From },
            { "To", e.To }
        });
        private readonly EventHandler<EventArgs> DomeSyncedHandler = async (_, e) => await WebSocketV2.SendAndAddEvent("DOME-SYNCED");

        public void Dispose()
        {
            AdvancedAPI.Controls.Dome.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Dome.Connected += DomeConnectedHandler;
            AdvancedAPI.Controls.Dome.Disconnected += DomeDisconnectedHandler;
            AdvancedAPI.Controls.Dome.Closed += DomeClosedHandler;
            AdvancedAPI.Controls.Dome.Opened += DomeOpenedHandler;
            AdvancedAPI.Controls.Dome.Homed += DomeHomedHandler;
            AdvancedAPI.Controls.Dome.Parked += DomeParkedHandler;
            AdvancedAPI.Controls.Dome.Slewed += DomeSlewedHandler;
            AdvancedAPI.Controls.Dome.Synced += DomeSyncedHandler;
            AdvancedAPI.Controls.Dome.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Dome.Connected -= DomeConnectedHandler;
            AdvancedAPI.Controls.Dome.Disconnected -= DomeDisconnectedHandler;
            AdvancedAPI.Controls.Dome.Closed -= DomeClosedHandler;
            AdvancedAPI.Controls.Dome.Opened -= DomeOpenedHandler;
            AdvancedAPI.Controls.Dome.Homed -= DomeHomedHandler;
            AdvancedAPI.Controls.Dome.Parked -= DomeParkedHandler;
            AdvancedAPI.Controls.Dome.Slewed -= DomeSlewedHandler;
            AdvancedAPI.Controls.Dome.Synced -= DomeSyncedHandler;
            AdvancedAPI.Controls.Dome.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(DomeInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("DOME");
        }
    }

    public class ExtendedDomeInfo : DomeInfo
    {
        public ExtendedDomeInfo(DomeInfo info, IDomeFollower follower)
        {
            Azimuth = info.Azimuth;
            CanFindHome = info.CanFindHome;
            CanPark = info.CanPark;
            CanSetAzimuth = info.CanSetAzimuth;
            CanSetPark = info.CanSetPark;
            CanSetShutter = info.CanSetShutter;
            CanSyncAzimuth = info.CanSyncAzimuth;
            DriverCanFollow = info.DriverCanFollow;
            DriverFollowing = info.DriverFollowing;
            ShutterStatus = info.ShutterStatus;
            AtHome = info.AtHome;
            AtPark = info.AtPark;
            Slewing = info.Slewing;
            SupportedActions = info.SupportedActions;
            IsFollowing = follower.IsFollowing;
            IsSynchronized = follower.IsSynchronized;
            DriverInfo = info.DriverInfo;
            DriverVersion = info.DriverVersion;
            Name = info.Name;
            DisplayName = info.DisplayName;
            Connected = info.Connected;
            Description = info.Description;
            DeviceId = info.DeviceId;
        }
        public bool IsFollowing { get; set; }
        public bool IsSynchronized { get; set; }
    }

    public partial class ControllerV2
    {
        private static CancellationTokenSource DomeToken;
        private static CancellationTokenSource FollowToken;


        [Route(HttpVerbs.Get, "/equipment/dome/info")]
        public void DomeInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                response.Response = new ExtendedDomeInfo(dome.GetInfo(), AdvancedAPI.Controls.DomeFollower);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/open")]
        public void DomeOpen()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else if (dome.GetInfo().ShutterStatus == ShutterState.ShutterOpen || dome.GetInfo().ShutterStatus == ShutterState.ShutterOpening)
                {
                    response.Response = "Shutter already open";
                }
                else
                {
                    DomeToken?.Cancel();
                    DomeToken = new CancellationTokenSource();
                    dome.OpenShutter(DomeToken.Token);
                    response.Response = "Shutter opening";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/close")]
        public void DomeClose()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else if (dome.GetInfo().ShutterStatus == ShutterState.ShutterClosed || dome.GetInfo().ShutterStatus == ShutterState.ShutterClosing)
                {
                    response.Response = "Shutter already closed";
                }
                else
                {
                    DomeToken?.Cancel();
                    DomeToken = new CancellationTokenSource();
                    dome.CloseShutter(DomeToken.Token);
                    response.Response = "Shutter closing";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        /// <summary>
        /// This only works if the dome movement was started using the API
        /// </summary>
        [Route(HttpVerbs.Get, "/equipment/dome/stop")]
        public void DomeStop()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else
                {
                    DomeToken?.Cancel();
                    response.Response = "Movement stopped";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/set-follow")]
        public async Task DomeEnableFollow([QueryField] bool enabled)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else
                {
                    if (enabled)
                    {
                        FollowToken?.Cancel();
                        FollowToken = new CancellationTokenSource();
                    }

                    response.Success = enabled ? await dome.EnableFollowing(FollowToken.Token) : await dome.DisableFollowing(FollowToken.Token);
                    response.Response = enabled ? "Following enabled" : "Following disabled";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/sync")]
        public void DomeSync()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else
                {
                    dome.SyncToScopeCoordinates(mount.GetInfo().Coordinates, mount.GetInfo().SideOfPier, new CancellationTokenSource().Token);
                    response.Response = "Dome Sync Started";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/slew")]
        public async Task DomeSlew([QueryField] double azimuth, [QueryField] bool waitToFinish)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;
                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else
                {
                    DomeToken?.Cancel();
                    DomeToken = new CancellationTokenSource();
                    await dome.DisableFollowing(CancellationToken.None);
                    if (waitToFinish)
                    {
                        await dome.SlewToAzimuth(azimuth, DomeToken.Token);
                        response.Response = "Dome Slew finished";
                    }
                    else
                    {
                        dome.SlewToAzimuth(azimuth, DomeToken.Token);
                        response.Response = "Dome Slew Started";
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

        [Route(HttpVerbs.Get, "/equipment/dome/slew/stop")]
        public void DomeStopSlew()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else if (!dome.GetInfo().Slewing)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not slewing", 409));
                }
                else
                {
                    DomeToken?.Cancel();
                    response.Response = "Stopped slew";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/set-park-position")]
        public void DomeSetPark()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 400));
                }
                else if (!dome.GetInfo().CanSetPark || dome.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome can not set park position", 400));
                }
                else
                {
                    var vm = typeof(DomeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dome) as DomeVM;
                    vm.SetParkPositionCommand.Execute(null);
                    response.Response = "Park position set";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/park")]
        public void DomePark()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 400));
                }
                else if (dome.GetInfo().AtPark)
                {
                    response.Response = "Dome already parked";
                }
                else
                {
                    DomeToken?.Cancel();
                    DomeToken = new CancellationTokenSource();
                    dome.Park(DomeToken.Token);
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

        [Route(HttpVerbs.Get, "/equipment/dome/home")]
        public void DomeHome()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 400));
                }
                else if (dome.GetInfo().AtHome)
                {
                    response.Response = "Mount already homed";
                }
                else
                {
                    DomeToken?.Cancel();
                    DomeToken = new CancellationTokenSource();
                    dome.FindHome(DomeToken.Token);
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
    }
}
