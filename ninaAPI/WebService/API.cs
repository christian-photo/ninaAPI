#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Extensions.DependencyInjection;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using ninaAPI.Properties;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V2;
using SimpleW;
using SimpleW.Helper.BasicAuth;
using SimpleW.Helper.DependencyInjection;
using SimpleW.Modules;
using SimpleW.Service.BasicAuth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ninaAPI.WebService
{
    public class WebApiServer : IWebApiServer
    {
        public SimpleWServer Server;

        public readonly int Port;

        public WebApiServer(int port)
        {
            Port = port;
        }

        private void CreateServer(ServiceProvider provider)
        {
            var serializer = SerializerFactory.GetSerializer();

            Server = new SimpleWServer(IPAddress.Any, Port);
            if (Settings.Default.UseAccessControlHeader)
            {
                Server.UseCorsModule(options =>
                {
                    options.AllowedOrigins = ["*"];
                    options.AllowedMethods = "GET, POST, PUT, DELETE, OPTIONS";
                });
            }
            Server.OnStarted((server) =>
            {
                Started?.Invoke(this, EventArgs.Empty);
            });
            Server.OnStopped((server) =>
            {
                Stopped?.Invoke(this, EventArgs.Empty);
            });
            if (Settings.Default.UseAuth)
            {
                if (string.IsNullOrEmpty(Settings.Default.AuthUsername) || string.IsNullOrEmpty(Settings.Default.AuthPassword))
                {
                    Notification.ShowWarning("Authentication is enabled but username or password is empty, disabling authentication");
                    Logger.Warning("Authentication is enabled but username or password is empty, disabling authentication");
                }
                else
                {
                    Server.UseBasicAuthModule(options =>
                    {
                        options.Users = [
                            new BasicUser(Settings.Default.AuthUsername, Settings.Default.AuthPassword)
                        ];
                    });
                }

            }
            if (Settings.Default.UseSSL)
            {
                try
                {
                    var cert = X509CertificateLoader.LoadPkcs12FromFile(Settings.Default.SSLCertificatePath, Settings.Default.SSLPassword);
                    var context = new SslContext(SslProtocols.Tls12 | SslProtocols.Tls13, cert, false, false);
                    Server.UseHttps(context);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load SSL certificate: {ex}");
                    Notification.ShowError("Failed to load SSL certificate, please check the logs for more info");
                }
            }
            Server.UseMiddleware(async (session, next) =>
            {
                try
                {
                    await next();
                }
                catch (HttpException ex)
                {
                    Logger.Error(ex.Message);
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
            Server.UseDependencyInjection(provider);
        }

        private static async Task HandleHttpException(HttpSession session, HttpException exception)
        {
            Logger.Trace($"Handling HttpException, status code: {exception.StatusCode}, Message: {exception.Message}");

            string error = HttpUtility.StatusCodeMessages.GetValueOrDefault((int)exception.StatusCode, "Unknown Error");

            var serializer = SerializerFactory.GetSerializer();

            await session.Response.Status((int)exception.StatusCode).Text(serializer.Serialize(new { Error = error, Message = exception.Message }), serializer.MimeType).SendAsync();
        }

        public async Task Start(ServiceProvider provider, params IHttpApi[] apis)
        {
            try
            {
                CreateServer(provider);
                foreach (IHttpApi api in apis)
                {
                    Server = api.ConfigureServer(Server, provider);
                }

                foreach (var route in Server.Router.Routes)
                {
                    Logger.Trace($"Registered Route: {route.Path}, Method: {route.Method}, Host: {route.Host}");
                }

                Logger.Info("Starting web server");
                await Server.StartAsync();
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
