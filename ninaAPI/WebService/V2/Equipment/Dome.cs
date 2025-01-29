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
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.WebService.V2.Equipment;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
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

        public void UpdateDeviceInfo(DomeInfo deviceInfo)
        {
            WebSocketV2.SendConsumerEvent("DOME");
        }
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

                DomeInfo info = dome.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/connect")]
        public async Task DomeConnect([QueryField] bool skipRescan)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    if (!skipRescan)
                    {
                        await dome.Rescan();
                    }
                    await dome.Connect();
                }
                response.Response = "Dome connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/dome/disconnect")]
        public async Task DomeDisconnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (dome.GetInfo().Connected)
                {
                    await dome.Disconnect();
                }
                response.Response = "Dome disconnected";
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
                    FollowToken?.Cancel();
                    FollowToken = new CancellationTokenSource();
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
        public void DomeSlew([QueryField] double azimuth)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (!dome.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                else if (dome.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Dome is parked", 409));
                }
                else
                {
                    dome.SlewToAzimuth(azimuth, new CancellationTokenSource().Token);
                    response.Response = "Dome Slew Started";
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
