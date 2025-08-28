#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.WebApi;
using ninaApi.Utility.Serialization;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Equipment.Camera;

namespace ninaAPI.WebService.V3
{
    public class V3Api : IHttpApi
    {
        private readonly ResponseHandler responseHandler;

        public V3Api()
        {
            responseHandler = new ResponseHandler(new NewtonsoftSerializer());
        }

        public WebServer ConfigureServer(WebServer server)
        {
            return server.WithWebApi("/v3/api/equipment/camera", m => m.WithController(() => new CameraController(
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.Imaging,
                AdvancedAPI.Controls.ImageSaveMediator,
                AdvancedAPI.Controls.StatusMediator,
                AdvancedAPI.Controls.ImageDataFactory,
                AdvancedAPI.Controls.PlateSolver,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.FilterWheel,
                responseHandler
            )))
                .WithWebApi("/v3/api", m => m.WithController<ControllerV3>());
        }

        public bool SupportsSSL() => true;
    }
}