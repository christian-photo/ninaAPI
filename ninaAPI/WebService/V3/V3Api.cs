#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
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
            return server.WithWebApi("/v3/api/equipment/camera", m => m.WithController<CameraController>())
                .WithWebApi("/v3/api", m => m.WithController<ControllerV3>())
                .HandleHttpException(HandleHttpException);
        }

        public bool SupportsSSL() => true;

        public async Task HandleHttpException(IHttpContext context, IHttpException exception)
        {
            Logger.Trace($"Handling HttpException, status code: {exception.StatusCode}, Message: {exception.Message}");
            exception.PrepareResponse(context);

            string error = HttpUtility.StatusCodeMessages.GetValueOrDefault(exception.StatusCode, "Unknown Error");

            string msg = exception.Message;
            object response = null;

            if (string.IsNullOrEmpty(msg))
            {
                response = new { Error = error };
            }
            else
            {
                response = new { Error = error, Message = msg };
            }
            await context.WriteResponse(response, exception.StatusCode);
        }
    }
}