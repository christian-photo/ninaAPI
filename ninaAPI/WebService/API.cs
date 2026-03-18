#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V2;
using SimpleW;
using SimpleW.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace ninaAPI.WebService
{
    public class WebApiServer : IWebApiServer
    {
        public SimpleWServer Server;

        public readonly int Port;

        private static List<INinaWatcher> Watchers { get; set; } = new List<INinaWatcher>();

        private ResponseHandler responseHandler;

        public WebApiServer(int port)
        {
            Port = port;
        }

        private void CreateServer()
        {
            var serializer = SerializerFactory.GetSerializer();
            responseHandler = new ResponseHandler(serializer);

            Server = new SimpleWServer(IPAddress.Any, Port).UseCorsModule(options =>
            {
                options.AllowAnyOrigin = true;
            });
            Server.UseMiddleware(async (session, next) =>
            {
                Logger.Debug($"Request: {session.Request.Path}", "AdvancedAPI.Middleware");
                using var sw = MyStopWatch.Measure("AdvancedAPI.Middleware");

                try
                {
                    await next();
                }
                catch (HttpException ex)
                {
                    await HandleHttpException(session, ex);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    if (ex is ArgumentException || ex is ValidationException)
                    {
                        await HandleHttpException(session, new HttpException(HttpStatusCode.BadRequest, ex.Message));
                    }
                    else
                    {
                        await HandleHttpException(session, CommonErrors.UnknwonError(ex));
                    }
                }
            });
        }

        private static async Task HandleHttpException(HttpSession session, HttpException exception)
        {
            Logger.Trace($"Handling HttpException, status code: {exception.StatusCode}, Message: {exception.Message}");

            string error = HttpUtility.StatusCodeMessages.GetValueOrDefault((int)exception.StatusCode, "Unknown Error");

            await session.Response.Status((int)exception.StatusCode).Text(SerializerFactory.GetSerializer().Serialize(new { Error = error, Message = exception.Message })).SendAsync();
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

        public async Task Start(params IHttpApi[] apis)
        {
            try
            {
                CreateServer();
                foreach (IHttpApi api in apis)
                {
                    Server = api.ConfigureServer(Server);
                }

                foreach (var route in Server.Router.Routes)
                {
                    Logger.Debug("Registered Route: " + route.Path);
                }

                Logger.Info("Starting web server");
                if (Server != null)
                {
                    await Server.StartAsync();
                    Started?.Invoke(this, EventArgs.Empty); // Raise Started event
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Notification.ShowError("Webserver start failed, please check the logs for more info");
            }
        }

        public async Task Stop()
        {
            try
            {
                await Server?.StopAsync();
                Server = null;
                Stopped?.Invoke(this, EventArgs.Empty); // Raise Stopped event
                WebSocketV2.SetUnavailable();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to stop web server: {ex}");
            }
        }

        public bool IsRunning() => Server?.IsStarted ?? false;

        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Stopped;
    }
}
