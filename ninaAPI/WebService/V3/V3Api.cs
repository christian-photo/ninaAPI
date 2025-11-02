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
using ninaAPI.WebService.V3.Equipment.Focuser;
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

            focuserController = new FocuserController(
                    AdvancedAPI.Controls.Focuser,
                    AdvancedAPI.Controls.FilterWheel,
                    AdvancedAPI.Controls.StatusMediator,
                    AdvancedAPI.Controls.AutoFocusFactory,
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
                .WithWebApi("/v3/api", m => m.WithController(() => controller));
        }

        public bool SupportsSSL() => true;
    }
}