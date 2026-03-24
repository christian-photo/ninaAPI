#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V2.CustomDrivers;
using SimpleW;

namespace ninaAPI.WebService.V2
{
    public class V2Api : IHttpApi
    {
        public SimpleWServer ConfigureServer(SimpleWServer server)
        {
            server.MapController<HttpControllerV2>("/v2/api");
            return server.WithModule(new WebSocketV2("/v2/socket"))
                .WithModule(new TPPASocket("/v2/tppa"))
                .WithModule(new MountAxisMoveSocket("/v2/mount"))
                .WithModule(new NetworkedFilterWheelSocket("/v2/filterwheel"));
        }

        public bool SupportsSSL() => false;
    }

    public class HttpControllerV2 : Controller { }
}