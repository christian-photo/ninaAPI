#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraWatcher : EventWatcher, ICameraConsumer
    {
        private readonly ICameraMediator camera;
        public CameraWatcher(EventHistoryManager eventHistory, ICameraMediator camera) : base(eventHistory)
        {
            this.camera = camera;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            camera.RegisterConsumer(this);
            camera.Connected += CameraConnected;
            camera.Disconnected += CameraDisconnected;
            camera.DownloadTimeout += CameraDownloadTimeout;
        }

        public override void StopWatchers()
        {
            camera.RemoveConsumer(this);
            camera.Connected -= CameraConnected;
            camera.Disconnected -= CameraDisconnected;
            camera.DownloadTimeout -= CameraDownloadTimeout;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) { }

        private async Task CameraConnected(object sender, EventArgs e) => await SubmitAndStoreEvent("CAMERA-CONNECTED");
        private async Task CameraDisconnected(object sender, EventArgs e) => await SubmitAndStoreEvent("CAMERA-DISCONNECTED");
        private async Task CameraDownloadTimeout(object sender, EventArgs e) => await SubmitAndStoreEvent("CAMERA-DOWNLOAD-TIMEOUT");
    }
}