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
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Equipment.Camera;
using ninaAPI.WebService.V3.Equipment.Focuser;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3
{
    public class V3Api : IHttpApi
    {
        private readonly ResponseHandler responseHandler;
        private readonly ISerializerService serializer;

        private EventWebSocket eventSocket;

        public V3Api()
        {
            serializer = SerializerFactory.GetSerializer();
            responseHandler = new ResponseHandler(serializer);
        }

        public WebServer ConfigureServer(WebServer server)
        {
            eventSocket = new EventWebSocket("/v3/ws/events", serializer);

            return server.WithModule(eventSocket)
                .WithWebApi("/v3/api/equipment/camera", m => m.WithController(() => new CameraController(
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
                .WithWebApi("/v3/api/equipment/focuser", m => m.WithController(() => new FocuserController(
                    AdvancedAPI.Controls.Focuser,
                    responseHandler
                )))
                .WithWebApi("/v3/api", m => m.WithController(() => new ControllerV3(responseHandler)));
        }

        public bool SupportsSSL() => true;
    }
}