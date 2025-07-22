#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
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
using ninaAPI.WebService.V2.CustomDrivers;
using System;
using System.Collections.Generic;
using System.Text;
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

        private static List<INinaWatcher> Watchers { get; set; } = new List<INinaWatcher>();

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
                .WithModule(new TPPASocket("/v2/tppa"))
                .WithModule(new MountAxisMoveSocket("/v2/mount"))
                .WithModule(new NetworkedFilterWheelSocket("/v2/filterwheel"));
        }

        public static void StartWatchers()
        {
            Watchers.Add(new CameraWatcher());
            Watchers.Add(new DomeWatcher());
            Watchers.Add(new FilterWheelWatcher());
            Watchers.Add(new FlatDeviceWatcher());
            Watchers.Add(new FocuserWatcher());
            Watchers.Add(new GuiderWatcher());
            Watchers.Add(new MountWatcher());
            Watchers.Add(new RotatorWatcher());
            Watchers.Add(new SafetyWatcher());
            Watchers.Add(new SwitchWatcher());
            Watchers.Add(new WeatherWatcher());
            Watchers.Add(new ImageWatcher());
            Watchers.Add(new NinaLogWatcher());
            Watchers.Add(new LiveStackWatcher());
            Watchers.Add(new ProfileWatcher());
            Watchers.Add(new TSWatcher());
            Watchers.Add(new SequenceWatcher());

            foreach (INinaWatcher watcher in Watchers)
            {
                watcher.StartWatchers();
            }
        }

        public static void StopWatchers()
        {
            Logger.Info("Stopping all event watchers");
            foreach (INinaWatcher watcher in Watchers)
            {
                watcher.StopWatchers();
            }
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
                WebSocketV2.SetUnavailable();
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

        protected override async Task OnRequestAsync(IHttpContext context)
        {
            Logger.Trace($"Request: {context.Request.Url.OriginalString}");
            if (Settings.Default.UseAccessControlHeader)
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

                if (context.Request.HttpVerb == HttpVerbs.Options)
                {
                    context.Response.StatusCode = 200;
                    await context.SendStringAsync(string.Empty, "text/plain", Encoding.UTF8);
                }
            }
        }

        public override bool IsFinalHandler => false;
    }
}