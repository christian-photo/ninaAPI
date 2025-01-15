#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.WebService.V2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService
{
    public class API
    {
        public WebServer Server;

        private Thread serverThread;

        private CancellationTokenSource apiToken;
        public readonly int Port;

        public API(int port)
        {
            Port = port;
        }

        public void CreateServer()
        {
            Server = new WebServer(o => o
                .WithUrlPrefix($"http://*:{Port}")
                .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new PreprocessRequestModule())
                .WithWebApi("/v2/api", m => m.WithController<ControllerV2>())
                .WithModule(new WebSocketV2("/v2/socket"))
                .WithModule(new TPPASocket("/v2/tppa"));
        }

        public static void StartWatchers()
        {
            ControllerV2.StartCameraWatchers();
            ControllerV2.StartDomeWatchers();
            ControllerV2.StartFilterWheelWatchers();
            ControllerV2.StartFlatDeviceWatchers();
            ControllerV2.StartFocuserWatchers();
            ControllerV2.StartGuiderWatchers();
            ControllerV2.StartMountWatchers();
            ControllerV2.StartRotatorWatchers();
            ControllerV2.StartSafetyWatchers();
            ControllerV2.StartSwitchWatchers();
            ControllerV2.StartWeatherWatchers();
            ControllerV2.StartImageWatcher();
            ControllerV2.StartLogWatcher();
        }

        public static void StopWatchers()
        {
            ControllerV2.StopDomeWatchers();
            ControllerV2.StopCameraWatchers();
            ControllerV2.StopFilterWheelWatchers();
            ControllerV2.StopFlatDeviceWatchers();
            ControllerV2.StopFocuserWatchers();
            ControllerV2.StopGuiderWatchers();
            ControllerV2.StopMountWatchers();
            ControllerV2.StopRotatorWatchers();
            ControllerV2.StopSafetyWatchers();
            ControllerV2.StopSwitchWatchers();
            ControllerV2.StopWeatherWatchers();
            ControllerV2.StopImageWatcher();
            ControllerV2.StopLogWatcher();
        }

        public void Start()
        {
            try
            {
                Logger.Debug("Creating Webserver");
                CreateServer();
                Logger.Info("Starting Webserver");
                if (Server != null)
                {
                    serverThread = new Thread(() => APITask(Server));
                    serverThread.Name = "API Thread";
                    serverThread.SetApartmentState(ApartmentState.STA);
                    serverThread.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to start web server: {ex}");
            }
        }

        public void Stop()
        {
            try
            {
                apiToken?.Cancel();
                Server?.Dispose();
                Server = null;
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to stop API: {ex}");
            }
        }

        [STAThread]
        private void APITask(WebServer server)
        {
            string ipAdress = CoreUtility.GetLocalNames()["IPADRESS"];
            Logger.Info($"starting web server, listening at {ipAdress}:{Port}");

            try
            {
                apiToken = new CancellationTokenSource();
                server.RunAsync(apiToken.Token).Wait();
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to start web server: {ex}");
                Notification.ShowError($"Failed to start web server, see NINA log for details");

                Logger.Debug("aborting web server thread");
            }
        }
    }

    public class PreprocessRequestModule : WebModuleBase
    {
        public PreprocessRequestModule() : base("/")
        {
        }

        protected override Task OnRequestAsync(IHttpContext context)
        {
            Logger.Trace($"Request: {context.Request.Url.OriginalString}");
            if (Settings.Default.UseAccessControlHeader)
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            }
            return Task.CompletedTask;
        }

        public override bool IsFinalHandler => false;
    }
}