#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService
{
    public class WebApiServer : IWebApiServer
    {
        public WebServer Server;

        private Thread serverThread;

        private CancellationTokenSource apiToken;
        public readonly int Port;

        private static List<INinaWatcher> Watchers { get; set; } = new List<INinaWatcher>();

        public WebApiServer(int port)
        {
            Port = port;
        }

        private void CreateServer()
        {
            Server = new WebServer(o => o
                .WithUrlPrefix($"http://*:{Port}")
                .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new PreprocessRequestModule());
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

        public void Start(params IHttpApi[] apis)
        {
            try
            {
                CreateServer();
                foreach (IHttpApi api in apis)
                {
                    Server = api.ConfigureServer(Server);
                }

                Logger.Info("Starting web server");
                if (Server != null)
                {
                    serverThread = new Thread(() => APITask(Server));
                    serverThread.Name = "API Thread";
                    serverThread.SetApartmentState(ApartmentState.STA);
                    serverThread.Start();
                    Started?.Invoke(this, EventArgs.Empty); // Raise Started event
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start web server: {ex}");
            }
        }

        public void Stop()
        {
            try
            {
                apiToken?.Cancel();
                Server?.Dispose();
                Server = null;
                Stopped?.Invoke(this, EventArgs.Empty); // Raise Stopped event
                WebSocketV2.SetUnavailable();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to stop web server: {ex}");
            }
        }

        [STAThread]
        private void APITask(WebServer server)
        {
            Logger.Info($"Starting web server, listening at {LocalAddresses.IPAddress}:{Port}");

            try
            {
                apiToken = new CancellationTokenSource();
                server.RunAsync(apiToken.Token).Wait();
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to start web server: {ex}");
                Notification.ShowError($"Failed to start web server, see NINA log for details");
            }
        }

        public bool IsRunning() => apiToken?.Token.IsCancellationRequested ?? false;

        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Stopped;
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