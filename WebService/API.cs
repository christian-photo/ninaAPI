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
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace ninaAPI.WebService
{
    public class API
    {
        public WebServer ActiveServer;
        private CancellationTokenSource apiToken;
        public readonly int Port;
        private Thread serverThread;
        public API()
        {
            Port = Settings.Default.Port;
            Start();
        }

        public WebServer CreateServer()
        {
            if (Settings.Default.Secure)
            {
                // X509Certificate2 certificate = new X509Certificate2();
                ActiveServer = new WebServer(o => o
                   .WithUrlPrefix($"https://*:{Port}")
                   .WithMode(HttpListenerMode.EmbedIO)
                   // .WithCertificate(certificate)
                   // .WithAutoRegisterCertificate()
                   );
                ActiveServer.WithWebApi($"/api/{Settings.Default.ApiKey}", m => m.WithController<Controller>());
                return ActiveServer;
            }
            ActiveServer = new WebServer(o => o
               .WithUrlPrefix($"http://*:{Port}")
               .WithMode(HttpListenerMode.EmbedIO)
               );
            
            ActiveServer.WithWebApi($"/api", m => m.WithController<Controller>());
            return ActiveServer;
        }

        public void Start()
        {
            try
            {
                serverThread = new Thread(APITask);
                serverThread.Name = "API Thread";
                serverThread.SetApartmentState(ApartmentState.STA);
                serverThread.Start();
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
                if (ActiveServer != null)
                {
                    if (ActiveServer.State != WebServerState.Stopped)
                    {
                        apiToken.Cancel();
                        ActiveServer.Dispose();
                        ActiveServer = null;
                    }
                }

                if (serverThread != null && serverThread.IsAlive)
                {
                    serverThread.Abort();
                    serverThread = null;
                }

                Notification.ShowSuccess($"API stopped");
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to stop API: {ex}");
            }
        }

        private void APITask()
        {
            string ipAdress = Utility.GetLocalNames()["IPADRESS"];
            Logger.Info($"starting web server, listening at {ipAdress}:{Port}");

            try
            {
                using (WebServer webServer = CreateServer())
                {
                    apiToken = new CancellationTokenSource();
                    Notification.ShowSuccess($"API started, listening at:\n {ipAdress}:{Port}");
                    webServer.RunAsync(apiToken.Token).Wait();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"failed to start web server: {ex}");
                Notification.ShowError($"Failed to start web server, see NINA log for details");

                Logger.Debug("aborting web server thread");
                serverThread.Abort();
            }
        }
    }
}