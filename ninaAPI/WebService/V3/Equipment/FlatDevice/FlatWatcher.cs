#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public sealed class FlatWatcher : EventWatcher, IFlatDeviceConsumer
    {
        private readonly IFlatDeviceMediator flatDevice;

        public FlatWatcher(EventHistoryManager history, IFlatDeviceMediator flatDevice) : base(history)
        {
            this.flatDevice = flatDevice;
            Channel = WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            flatDevice.Connected += FlatDeviceConnectedHandler;
            flatDevice.Disconnected += FlatDeviceDisconnectedHandler;
            flatDevice.LightToggled += FlatDeviceLightToggledHandler;
            flatDevice.Opened += FlatDeviceOpenedHandler;
            flatDevice.Closed += FlatDeviceClosedHandler;
            flatDevice.BrightnessChanged += FlatDeviceBrightnessChangedHandler;
            flatDevice.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            flatDevice.Connected -= FlatDeviceConnectedHandler;
            flatDevice.Disconnected -= FlatDeviceDisconnectedHandler;
            flatDevice.LightToggled -= FlatDeviceLightToggledHandler;
            flatDevice.Opened -= FlatDeviceOpenedHandler;
            flatDevice.Closed -= FlatDeviceClosedHandler;
            flatDevice.BrightnessChanged -= FlatDeviceBrightnessChangedHandler;
            flatDevice.RemoveConsumer(this);
        }

        private async Task FlatDeviceConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceConnected(Device.FlatDevice));
        private async Task FlatDeviceDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceDisconnected(Device.FlatDevice));
        private async Task FlatDeviceLightToggledHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.FLATDEVICE_LIGHT_TOGGLED);
        private async Task FlatDeviceOpenedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.FLATDEVICE_OPENED);
        private async Task FlatDeviceClosedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.FLATDEVICE_CLOSED);
        private async Task FlatDeviceBrightnessChangedHandler(object sender, FlatDeviceBrightnessChangedEventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.FLATDEVICE_BRIGHTNESS_CHANGED, new
        {
            e.From,
            e.To,
        });

        public async void UpdateDeviceInfo(FlatDeviceInfo deviceInfo)
        {
            await SubmitEvent(WebSocketEvents.DeviceInfoUpdate(Device.FlatDevice), new FlatInfoResponse(flatDevice), WebSocketChannel.FlatdeviceInfoUpdate);
        }
    }
}
