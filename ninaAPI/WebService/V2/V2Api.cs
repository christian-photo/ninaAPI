#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V2.CustomDrivers;
using SimpleW;
using SimpleW.Modules;

namespace ninaAPI.WebService.V2
{
    public class V2Api : IHttpApi
    {
        private static List<INinaWatcher> Watchers { get; set; } = new List<INinaWatcher>();

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
            foreach (INinaWatcher watcher in Watchers)
            {
                watcher.StopWatchers();
            }
        }

        public bool SupportsSSL() => false;
    }
}