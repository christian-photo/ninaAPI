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
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V2.CustomDrivers;

namespace ninaAPI.WebService.V2
{
    public class V2Api : IHttpApi
    {
        public WebServer ConfigureServer(WebServer server)
        {
            return server.WithWebApi("/v2/api", m => m.WithController<ControllerV2>())
                .WithModule(new WebSocketV2("/v2/socket"))
                .WithModule(new TPPASocket("/v2/tppa"))
                .WithModule(new MountAxisMoveSocket("/v2/mount"))
                .WithModule(new NetworkedFilterWheelSocket("/v2/filterwheel"));
        }

        public bool SupportsSSL() => false;
    }
}