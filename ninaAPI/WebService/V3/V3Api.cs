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
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Application;
using ninaAPI.WebService.V3.Application.Framing;
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
using SimpleW.Modules;

namespace ninaAPI.WebService.V3
{
    public class V3Api : IHttpApi
    {
        private readonly ResponseHandler responseHandler;
        private readonly ISerializerService serializer;

        private readonly CameraController cameraController;
        private readonly FocuserController focuserController;
        private readonly DomeController domeController;
        private readonly FilterWheelController filterWheelController;
        private readonly FlatController flatController;
        private readonly GuiderController guiderController;
        private readonly MountController mountController;
        private readonly RotatorController rotatorController;
        private readonly SafetyController safetyController;
        private readonly SwitchController switchController;
        private readonly WeatherController weatherController;
        private readonly DeviceController connectionController;

        private readonly ApplicationController applicationController;
        private readonly ImageController imageController;
        private readonly ProfileController profileController;
        private readonly SequenceController sequenceController;
        private readonly FramingController framingController;
        private readonly ControllerV3 controller;
        private readonly ApiProcessMediator processMediator;

        private readonly LivestackController livestackController;
        private readonly TppaController tppaController;

        private static EventHistoryManager eventHistory;
        private EventWebSocket eventSocket;
        private static List<EventWatcher> watchers;

        private readonly MountControlSocket mountControlSocket;


        // TODO: Missing endpoints / watchers
        // - Flat
        // - Networked filterwheel
        // - Networked rotator

        public static void StartEventWatchers()
        {
            eventHistory = new EventHistoryManager();
            watchers =
            [
                new CameraWatcher(eventHistory, AdvancedAPI.Controls.Camera),
                new DomeWatcher(eventHistory, AdvancedAPI.Controls.Dome, AdvancedAPI.Controls.DomeFollower),
                new FilterWheelWatcher(eventHistory, AdvancedAPI.Controls.FilterWheel, AdvancedAPI.Controls.Profile),
                new FlatWatcher(eventHistory, AdvancedAPI.Controls.FlatDevice),
                new FocuserWatcher(eventHistory, AdvancedAPI.Controls.Focuser),
                new GuiderWatcher(eventHistory, AdvancedAPI.Controls.Guider),
                new MountWatcher(eventHistory, AdvancedAPI.Controls.Mount),
                new RotatorWatcher(eventHistory, AdvancedAPI.Controls.Rotator),
                new SafetyWatcher(eventHistory, AdvancedAPI.Controls.SafetyMonitor),
                new SwitchWatcher(eventHistory, AdvancedAPI.Controls.Switch),
                new WeatherWatcher(eventHistory, AdvancedAPI.Controls.Weather),
                new ProcessWatcher(eventHistory),
                new ImageWatcher(eventHistory, AdvancedAPI.Controls.ImageSaveMediator, AdvancedAPI.Controls.Imaging),
                new ProfileWatcher(eventHistory, AdvancedAPI.Controls.Profile),
                new SequenceWatcher(eventHistory, AdvancedAPI.Controls.Sequence),
                new LivestackWatcher(eventHistory, AdvancedAPI.Controls.MessageBroker),
                new TppaWatcher(eventHistory, AdvancedAPI.Controls.MessageBroker),
                new TSWatcher(eventHistory, AdvancedAPI.Controls.MessageBroker),
            ];

            foreach (EventWatcher watcher in watchers)
            {
                watcher.StartWatchers();
            }
        }

        public static void StopEventWatchers()
        {
            foreach (EventWatcher watcher in watchers)
            {
                watcher.StopWatchers();
            }
        }

        public V3Api()
        {
            serializer = SerializerFactory.GetSerializer();
            responseHandler = new ResponseHandler(serializer);
            processMediator = new ApiProcessMediator();

            cameraController = new CameraController(
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.Imaging,
                AdvancedAPI.Controls.ImageSaveMediator,
                AdvancedAPI.Controls.StatusMediator,
                AdvancedAPI.Controls.ImageDataFactory,
                AdvancedAPI.Controls.PlateSolver,
                AdvancedAPI.Controls.FilterWheel,
                processMediator,
                serializer
            );

            domeController = new DomeController(
                AdvancedAPI.Controls.Dome,
                AdvancedAPI.Controls.DomeFollower,
                AdvancedAPI.Controls.Mount,
                processMediator,
                serializer
            );

            filterWheelController = new FilterWheelController(
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.StatusMediator,
                processMediator,
                serializer
            );

            flatController = new FlatController(
                AdvancedAPI.Controls.FlatDevice,
                AdvancedAPI.Controls.StatusMediator,
                serializer
            );

            focuserController = new FocuserController(
                AdvancedAPI.Controls.Focuser,
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.StatusMediator,
                AdvancedAPI.Controls.AutoFocusFactory,
                processMediator,
                serializer
            );

            guiderController = new GuiderController(
                AdvancedAPI.Controls.Guider,
                AdvancedAPI.Controls.StatusMediator,
                processMediator,
                serializer
            );

            mountController = new MountController(
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.Imaging,
                AdvancedAPI.Controls.Rotator,
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.Guider,
                AdvancedAPI.Controls.Dome,
                AdvancedAPI.Controls.DomeFollower,
                AdvancedAPI.Controls.PlateSolver,
                AdvancedAPI.Controls.WindowFactory,
                AdvancedAPI.Controls.StatusMediator,
                AdvancedAPI.Controls.MeridianFlipFactory,
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Focuser,
                processMediator
            );

            rotatorController = new RotatorController(
                AdvancedAPI.Controls.Rotator,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.Imaging,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.PlateSolver,
                AdvancedAPI.Controls.WindowFactory,
                AdvancedAPI.Controls.StatusMediator,
                processMediator
            );

            safetyController = new SafetyController(
                AdvancedAPI.Controls.SafetyMonitor
            );

            switchController = new SwitchController(
                AdvancedAPI.Controls.Switch,
                AdvancedAPI.Controls.StatusMediator
            );

            weatherController = new WeatherController(
                AdvancedAPI.Controls.Weather
            );

            connectionController = new DeviceController(
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Dome,
                AdvancedAPI.Controls.DomeFollower,
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.FlatDevice,
                AdvancedAPI.Controls.Focuser,
                AdvancedAPI.Controls.Guider,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.Rotator,
                AdvancedAPI.Controls.SafetyMonitor,
                AdvancedAPI.Controls.Switch,
                AdvancedAPI.Controls.Weather,
                AdvancedAPI.Controls.Profile
            );

            imageController = new ImageController(
                AdvancedAPI.Controls.ImageDataFactory,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.PlateSolver,
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.StatusMediator
            );

            profileController = new ProfileController(
                AdvancedAPI.Controls.Profile
            );

            applicationController = new ApplicationController(
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.Application
            );

            sequenceController = new SequenceController(
                AdvancedAPI.Controls.Sequence,
                serializer
            );

            framingController = new FramingController(
                AdvancedAPI.Controls.FramingAssistant,
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Profile,
                processMediator
            );

            livestackController = new LivestackController(
                AdvancedAPI.Controls.MessageBroker,
                AdvancedAPI.Controls.Profile
            );

            tppaController = new TppaController(
                AdvancedAPI.Controls.MessageBroker
            );

            controller = new ControllerV3(processMediator);

            mountControlSocket = new MountControlSocket(AdvancedAPI.Controls.Mount, serializer);
        }

        public SimpleWServer ConfigureServer(SimpleWServer server)
        {
            eventSocket = new EventWebSocket(serializer, eventHistory);

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
                    .ContentType(serializer.MimeType)
                    .Text(json)
                    .SendAsync();
            });
            controller.Configure(server, "/v3/api");
            cameraController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.CameraUrlName}");
            domeController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.DomeUrlName}");
            filterWheelController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.FilterWheelUrlName}");
            focuserController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.FocuserUrlName}");
            flatController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.FlatDeviceUrlName}");
            guiderController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.GuiderUrlName}");
            mountController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.MountUrlName}");
            rotatorController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.RotatorUrlName}");
            safetyController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.SafetyMonitorUrlName}");
            switchController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.SwitchUrlName}");
            weatherController.Configure(server, $"/v3/api/equipment/{EquipmentConstants.WeatherUrlName}");
                .WithWebApi("/v3/api/image", m => m.WithController(() => imageController))
                .WithWebApi("/v3/api/profile", m => m.WithController(() => profileController))
                .WithWebApi("/v3/api/application", m => m.WithController(() => applicationController))
                .WithWebApi("/v3/api/sequence", m => m.WithController(() => sequenceController))
                .WithWebApi("/v3/api/framing", m => m.WithController(() => framingController))
                .WithWebApi("/v3/api/livestack", m => m.WithController(() => livestackController))
                .WithWebApi("/v3/api/tppa", m => m.WithController(() => tppaController))
                .WithWebApi("/v3/api", m => m.WithController(() => controller));

            return server;
        }

        public bool SupportsSSL() => true;

        public EventWebSocket GetEventWebSocket()
        {
            return eventSocket;
        }
    }
}
