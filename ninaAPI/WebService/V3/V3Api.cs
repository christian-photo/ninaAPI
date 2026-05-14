#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Application.Image;
using ninaAPI.WebService.V3.Application.Livestack;
using ninaAPI.WebService.V3.Application.Profile;
using ninaAPI.WebService.V3.Application.Sequence;
using ninaAPI.WebService.V3.Application.TPPA;
using ninaAPI.WebService.V3.Application.TS;
using ninaAPI.WebService.V3.Equipment;
using ninaAPI.WebService.V3.Equipment.Camera;
using ninaAPI.WebService.V3.Equipment.Dome;
using ninaAPI.WebService.V3.Equipment.FilterWheel;
using ninaAPI.WebService.V3.Equipment.FlatDevice;
using ninaAPI.WebService.V3.Equipment.Focuser;
using ninaAPI.WebService.V3.Equipment.Guider;
using ninaAPI.WebService.V3.Equipment.Mount;
using ninaAPI.WebService.V3.Equipment.Rotator;
using ninaAPI.WebService.V3.Equipment.Safety;
using ninaAPI.WebService.V3.Equipment.Switch;
using ninaAPI.WebService.V3.Equipment.Weather;
using ninaAPI.WebService.V3.Websocket.Event;
using ninaAPI.WebService.V3.Websocket.MountControl;
using SimpleW;
using SimpleW.Helper.DependencyInjection;
using SimpleW.Modules;

namespace ninaAPI.WebService.V3
{
    public class V3Api : IHttpApi
    {
        private static EventHistoryManager eventHistory;
        private EventWebSocket eventSocket;
        private static List<EventWatcher> watchers;

        private MountControlSocket mountControlSocket;


        // TODO: Missing endpoints / watchers
        // - Flat
        // - Networked filterwheel
        // - Networked rotator

        public static void StartWatchers(ServiceProvider provider)
        {
            eventHistory = new EventHistoryManager();
            watchers =
            [
                new CameraWatcher(eventHistory, provider.GetService<ICameraMediator>()),
                new DomeWatcher(eventHistory, provider.GetService<IDomeMediator>(), provider.GetService<IDomeFollower>()),
                new FilterWheelWatcher(eventHistory, provider.GetService<IFilterWheelMediator>(), provider.GetService<IProfileService>()),
                new FlatWatcher(eventHistory, provider.GetService<IFlatDeviceMediator>()),
                new FocuserWatcher(eventHistory, provider.GetService<IFocuserMediator>()),
                new GuiderWatcher(eventHistory, provider.GetService<IGuiderMediator>()),
                new MountWatcher(eventHistory, provider.GetService<ITelescopeMediator>()),
                new RotatorWatcher(eventHistory, provider.GetService<IRotatorMediator>()),
                new SafetyWatcher(eventHistory, provider.GetService<ISafetyMonitorMediator>()),
                new SwitchWatcher(eventHistory, provider.GetService<ISwitchMediator>()),
                new WeatherWatcher(eventHistory, provider.GetService<IWeatherDataMediator>()),
                new ProcessWatcher(eventHistory),
                new ImageWatcher(eventHistory, provider.GetService<IImageSaveMediator>(), provider.GetService<IImagingMediator>()),
                new ProfileWatcher(eventHistory, provider.GetService<IProfileService>()),
                new SequenceWatcher(eventHistory, provider.GetService<ISequenceMediator>()),
                new LivestackWatcher(eventHistory, provider.GetService<IMessageBroker>()),
                new TppaWatcher(eventHistory, provider.GetService<IMessageBroker>()),
                new TSWatcher(eventHistory, provider.GetService<IMessageBroker>()),
            ];

            foreach (EventWatcher watcher in watchers)
            {
                watcher.StartWatchers();
            }
        }

        public static void StopWatchers()
        {
            foreach (EventWatcher watcher in watchers)
            {
                watcher.StopWatchers();
            }
        }

        public SimpleWServer ConfigureServer(SimpleWServer server, ServiceProvider provider)
        {
            var serializer = provider.GetService<ISerializerService>();

            eventSocket = new EventWebSocket(serializer, eventHistory);
            mountControlSocket = new MountControlSocket(provider.GetService<ITelescopeMediator>(), serializer);

            foreach (EventWatcher watcher in watchers)
            {
                watcher.Initialize(eventSocket);
            }

            Directory.CreateDirectory(FileSystemHelper.GetProcessTempFolder());

            server = server.UseWebSocketModule(ws =>
            {
                ws.Prefix = "/v3/ws/events";

                eventSocket.ConfigureWebSocket(ws);
            })
            .UseWebSocketModule(ws =>
            {
                ws.Prefix = "/v3/ws/mount-control";

                mountControlSocket.ConfigureWebSocket(ws);
            })
            .ConfigureResultHandler(async (session, result) =>
            {
                object body;
                int statusCode;

                if (result is ITuple tuple && tuple.Length == 2 && tuple[1] is int code)
                {
                    body = tuple[0];
                    statusCode = code;
                }
                else
                {
                    body = result;
                    statusCode = 200;
                }

                var json = serializer.Serialize(body);

                await session.Response
                    .Status(statusCode)
                    .Text(json, serializer.MimeType)
                    .SendAsync();
            });

            server.MapController<ControllerV3>();

            // server.MapControllers<Controller>(excludes: [typeof(V2.ControllerV2)]);

            return server;
        }

        public bool SupportsSSL() => true;

        public EventWebSocket GetEventWebSocket()
        {
            return eventSocket;
        }
    }
}
