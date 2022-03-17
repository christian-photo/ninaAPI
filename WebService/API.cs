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
using System.Threading;

namespace ninaAPI.WebService
{
    public class API
    {
        public WebServer ActiveServer;
        private Thread serverThread;
        private CancellationTokenSource apiToken;

        public API()
        {
            Start();
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
            string localUrl = $"http://localhost:{Settings.Default.Port}";
            Logger.Info($"starting web server, listening at {localUrl}");

            try
            {
                using (WebServer webServer = CreateServer())
                {
                    apiToken = new CancellationTokenSource();
                    Notification.ShowSuccess($"API started, listening at:\n {localUrl}");
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

        public WebServer CreateServer()
        {
            ActiveServer = new WebServer(o => o
                               .WithUrlPrefix("http://localhost:1111")
                               .WithMode(HttpListenerMode.EmbedIO)
                               );
            ActiveServer.WithWebApi("/api", m => m.WithController<Controller>());
            return ActiveServer;
        }
    }
}
