#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.IO;
using EmbedIO;
using EmbedIO.WebApi;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
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
        private readonly ConnectController connectionController;
        private readonly ControllerV3 controller;
        private readonly ApiProcessMediator processMediator;

        private static EventHistoryManager eventHistory;
        private EventWebSocket eventSocket;
        private static List<EventWatcher> watchers;

        public static void StartEventWatchers()
        {
            eventHistory = new EventHistoryManager();
            watchers =
            [
                new CameraWatcher(eventHistory, AdvancedAPI.Controls.Camera),
                new FocuserWatcher(eventHistory, AdvancedAPI.Controls.Focuser),
                new DomeWatcher(eventHistory, AdvancedAPI.Controls.Dome, AdvancedAPI.Controls.DomeFollower),
                new FilterWheelWatcher(eventHistory, AdvancedAPI.Controls.FilterWheel, AdvancedAPI.Controls.Profile),
                new FlatWatcher(eventHistory, AdvancedAPI.Controls.FlatDevice),
                new GuiderWatcher(eventHistory, AdvancedAPI.Controls.Guider),
                new MountWatcher(eventHistory, AdvancedAPI.Controls.Mount),
                new RotatorWatcher(eventHistory, AdvancedAPI.Controls.Rotator),
                new SafetyWatcher(eventHistory, AdvancedAPI.Controls.SafetyMonitor),
                new SwitchWatcher(eventHistory, AdvancedAPI.Controls.Switch),
                new WeatherWatcher(eventHistory, AdvancedAPI.Controls.Weather),
                new ProcessWatcher(eventHistory),
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
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.Imaging,
                AdvancedAPI.Controls.ImageSaveMediator,
                AdvancedAPI.Controls.StatusMediator,
                AdvancedAPI.Controls.ImageDataFactory,
                AdvancedAPI.Controls.PlateSolver,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.FilterWheel,
                responseHandler,
                processMediator
            );

            domeController = new DomeController(
                responseHandler,
                AdvancedAPI.Controls.Dome,
                AdvancedAPI.Controls.DomeFollower,
                AdvancedAPI.Controls.Mount,
                processMediator
            );

            filterWheelController = new FilterWheelController(
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.Profile,
                AdvancedAPI.Controls.StatusMediator,
                responseHandler,
                processMediator
            );

            flatController = new FlatController(
                AdvancedAPI.Controls.FlatDevice,
                AdvancedAPI.Controls.StatusMediator,
                responseHandler
            );

            focuserController = new FocuserController(
                AdvancedAPI.Controls.Focuser,
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.StatusMediator,
                AdvancedAPI.Controls.AutoFocusFactory,
                responseHandler,
                processMediator
            );

            guiderController = new GuiderController(
                AdvancedAPI.Controls.Guider,
                AdvancedAPI.Controls.StatusMediator,
                processMediator,
                responseHandler
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
                processMediator,
                responseHandler
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
                processMediator,
                responseHandler
            );

            safetyController = new SafetyController(
                AdvancedAPI.Controls.SafetyMonitor,
                responseHandler
            );

            switchController = new SwitchController(
                AdvancedAPI.Controls.Switch,
                AdvancedAPI.Controls.StatusMediator,
                responseHandler
            );

            weatherController = new WeatherController(
                AdvancedAPI.Controls.Weather,
                responseHandler
            );

            connectionController = new ConnectController(
                AdvancedAPI.Controls.Camera,
                AdvancedAPI.Controls.Dome,
                AdvancedAPI.Controls.FilterWheel,
                AdvancedAPI.Controls.FlatDevice,
                AdvancedAPI.Controls.Focuser,
                AdvancedAPI.Controls.Guider,
                AdvancedAPI.Controls.Mount,
                AdvancedAPI.Controls.Rotator,
                AdvancedAPI.Controls.SafetyMonitor,
                AdvancedAPI.Controls.Switch,
                AdvancedAPI.Controls.Weather,
                responseHandler
            );

            controller = new ControllerV3(responseHandler, processMediator);
        }

        public WebServer ConfigureServer(WebServer server)
        {
            eventSocket = new EventWebSocket("/v3/ws/events", serializer, eventHistory);

            foreach (EventWatcher watcher in watchers)
            {
                watcher.Initialize(eventSocket);
            }

            Directory.CreateDirectory(FileSystemHelper.GetProcessTempFolder());

            // EMBEDIO WOULD CREATE A NEW INSTANCE OF THE CONTROLLER FOR EACH REQUEST
            return server.WithModule(eventSocket)
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.CameraUrlName}", m => m.WithController(() => cameraController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.FocuserUrlName}", m => m.WithController(() => focuserController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.DomeUrlName}", m => m.WithController(() => domeController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.FilterWheelUrlName}", m => m.WithController(() => filterWheelController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.FlatDeviceUrlName}", m => m.WithController(() => flatController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.GuiderUrlName}", m => m.WithController(() => guiderController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.MountUrlName}", m => m.WithController(() => mountController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.RotatorUrlName}", m => m.WithController(() => rotatorController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.SafetyMonitorUrlName}", m => m.WithController(() => safetyController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.SwitchUrlName}", m => m.WithController(() => switchController))
                .WithWebApi($"/v3/api/equipment/{EquipmentConstants.WeatherUrlName}", m => m.WithController(() => weatherController))
                .WithWebApi("/v3/api/equipment", m => m.WithController(() => connectionController))
                .WithWebApi("/v3/api", m => m.WithController(() => controller));
        }

        public bool SupportsSSL() => true;
    }
}
