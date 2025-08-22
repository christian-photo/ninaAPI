#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.Utility;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Equipment;

namespace ninaAPI.WebService.V3
{
    public class V3Api : IHttpApi
    {
        public WebServer ConfigureServer(WebServer server)
        {
            return server.WithWebApi("/v3/api", m => m.WithController<ControllerV3>())
                .WithWebApi("/v3/api/equipment/camera", m => m.WithController<CameraController>())
                .HandleHttpException((context, exception) =>
                {
                    Logger.Trace($"Handling HttpException, status code: {exception.StatusCode}");
                    context.WriteResponse(new { Error = exception.Message }, 404);
                    return Task.CompletedTask;
                });
        }

        public bool SupportsSSL() => true;
    }
}