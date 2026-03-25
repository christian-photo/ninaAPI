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
using SimpleW.Modules;

namespace ninaAPI.WebService.V2
{
    public class V2Api : IHttpApi
    {
        public SimpleWServer ConfigureServer(SimpleWServer server)
        {
            server.MapController<ControllerV2>("/v2/api");

            server.UseWebSocketModule(o =>
            {
                o.Prefix = "/v2/socket";
                new WebSocketV2().ConfigureWebSocket(o);
            });
            server.UseWebSocketModule(o =>
            {
                o.Prefix = "/v2/tppa";
                new TPPASocket().ConfigureWebSocket(o);
            });
            server.UseWebSocketModule(o =>
            {
                o.Prefix = "/v2/mount";
                new MountAxisMoveSocket().ConfigureWebSocket(o);
            });
            server.UseWebSocketModule(o =>
            {
                o.Prefix = "/v2/filterwheel";
                new NetworkedFilterWheelSocket().ConfigureWebSocket(o);
            });
            server.UseWebSocketModule(o =>
            {
                o.Prefix = "/v2/rotator";
                new NetworkedRotatorSocket().ConfigureWebSocket(o);
            });

            return server;
        }

        public bool SupportsSSL() => false;
    }
}