#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
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
using ninaAPI.WebService.V1;
using ninaAPI.WebService.V2;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

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
            if (Settings.Default.Secure)
            {
                Server = new WebServer(o => o
                   .WithUrlPrefix($"https://*:{Port}")
                   .WithMode(HttpListenerMode.EmbedIO)
                   .WithAutoLoadCertificate()
                   .WithCertificate(new X509Certificate2(Settings.Default.CertificatePath, Settings.Default.CertificatePassword)));
            }
            else
            {
                Server = new WebServer(o => o
                   .WithUrlPrefix($"http://*:{Port}")
                   .WithMode(HttpListenerMode.EmbedIO));
            }

            if (Settings.Default.StartV1)
            {
                Server.WithWebApi($"/api", m => m.WithController<Controller>());
                Server.WithModule(new WebSocket("/socket"));
            }

            if (Settings.Default.StartV2)
            {
                Server.WithWebApi($"/v2/api", m => m.WithController<ControllerV2>());
                Server.WithModule(new WebSocketV2("/v2/socket"));
            }
        }

        public void Start()
        {
            if (Settings.Default.Secure && (string.IsNullOrEmpty(Settings.Default.ApiKey) || string.IsNullOrEmpty(Settings.Default.CertificatePath)))
            {
                Logger.Error("Secure API is enabled but no certificate or key is set. Please set the certificate and key in the settings.");
                Notification.ShowError("Secure API is enabled but no certificate or key is set. Please set the certificate and key in the settings.");
                return;
            }
            try
            {
                LogWatcher = new NINALogWatcher(LogProcessor);
                LogWatcher.Start();


                Logger.Debug("Creating Webserver");
                CreateServer();
                Logger.Info("Starting Webserver");
                if (Server != null)
                {
                    serverThread = new Thread(() => APITask(Server));
                    serverThread.Name = "API Thread V1";
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
            string ipAdress = Utility.GetLocalNames()["IPADRESS"];
            Logger.Info($"starting web server, listening at {ipAdress}:{Port}");

            try
            {
                apiToken = new CancellationTokenSource();
                server.RunAsync(apiToken.Token).Wait();
                Notification.ShowSuccess($"API started, listening at:\n {ipAdress}:{Port}");
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to start web server: {ex}");
                Notification.ShowError($"Failed to start web server, see NINA log for details");

                Logger.Debug("aborting web server thread");
            }
        }
    }
}