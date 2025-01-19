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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        private static CancellationTokenSource DomeToken;

        private static readonly Func<object, EventArgs, Task> DomeConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-CONNECTED");
        private static readonly Func<object, EventArgs, Task> DomeDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-DISCONNECTED");
        private static readonly Func<object, EventArgs, Task> DomeClosedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-SHUTTER-CLOSED");
        private static readonly Func<object, EventArgs, Task> DomeOpenedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-SHUTTER-OPENED");
        private static readonly Func<object, EventArgs, Task> DomeHomedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-HOMED");
        private static readonly Func<object, EventArgs, Task> DomeParkedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("DOME-PARKED");

        public static void StartDomeWatchers()
        {
            AdvancedAPI.Controls.Dome.Connected += DomeConnectedHandler;
            AdvancedAPI.Controls.Dome.Disconnected += DomeDisconnectedHandler;
            AdvancedAPI.Controls.Dome.Closed += DomeClosedHandler;
            AdvancedAPI.Controls.Dome.Opened += DomeOpenedHandler;
            AdvancedAPI.Controls.Dome.Homed += DomeHomedHandler;
            AdvancedAPI.Controls.Dome.Parked += DomeParkedHandler;
        }

        public static void StopDomeWatchers()
        {
            AdvancedAPI.Controls.Dome.Connected -= DomeConnectedHandler;
            AdvancedAPI.Controls.Dome.Disconnected -= DomeDisconnectedHandler;
            AdvancedAPI.Controls.Dome.Closed -= DomeClosedHandler;
            AdvancedAPI.Controls.Dome.Opened -= DomeOpenedHandler;
            AdvancedAPI.Controls.Dome.Homed -= DomeHomedHandler;
            AdvancedAPI.Controls.Dome.Parked -= DomeParkedHandler;
        }


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
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterOpen || dome.GetInfo().ShutterStatus == ShutterState.ShutterOpening)
                {
                    response.Response = "Shutter already open";
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.OpenShutter(DomeToken.Token);
                response.Response = "Shutter opening";
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
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterClosed || dome.GetInfo().ShutterStatus == ShutterState.ShutterClosing)
                {
                    response.Response = "Shutter already closed";
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.CloseShutter(DomeToken.Token);
                response.Response = "Shutter closing";
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

                DomeToken?.Cancel();
                response.Response = "Movement stopped";
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
