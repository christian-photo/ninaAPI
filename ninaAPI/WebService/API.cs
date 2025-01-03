﻿#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Cors;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.WebService.V1;
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

        public NINALogMessageProcessor LogProcessor;
        public NINALogWatcher LogWatcher;
        private CancellationTokenSource apiToken;
        public readonly int Port;

        public SynchronizationContext SyncContext { get; private set; } = SynchronizationContext.Current;
        public API()
        {
            Port = Settings.Default.Port;
            LogProcessor = new NINALogMessageProcessor();
        }

        public void CreateServer()
        {
            Server = new WebServer(o => o
                .WithUrlPrefix($"http://*:{Port}")
                .WithMode(HttpListenerMode.EmbedIO));

            Server.WithModule(new CustomHeaderModule());

            if (Settings.Default.StartV1)
            {
                Server.WithWebApi("/api", m => m.WithController<Controller>());
                Server.WithModule(new WebSocket("/socket"));
            }

            if (Settings.Default.StartV2)
            {
                Server.WithWebApi("/v2/api", m => m.WithController<ControllerV2>());
                Server.WithModule(new WebSocketV2("/v2/socket"));
                Server.WithModule(new TPPASocket("/v2/tppa"));
            }
        }

        public void Start()
        {
            try
            {
                LogWatcher = new NINALogWatcher(LogProcessor);
                LogWatcher.Start();
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
                LogWatcher?.Stop();
                apiToken?.Cancel();
                Server?.Dispose();
                Server = null;

                Notification.ShowSuccess($"API stopped");
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

    public class CustomHeaderModule : WebModuleBase
    {
        public CustomHeaderModule() : base("/")
        {
        }

        protected override Task OnRequestAsync(IHttpContext context)
        {
            if (Settings.Default.UseAccessControlHeader)
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            }
            return Task.CompletedTask;
        }

        public override bool IsFinalHandler => false;
    }
}