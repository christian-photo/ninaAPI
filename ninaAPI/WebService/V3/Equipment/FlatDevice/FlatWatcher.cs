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
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public class FlatWatcher : EventWatcher, IFlatDeviceConsumer
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

        private async Task FlatDeviceConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("FLATDEVICE-CONNECTED");
        private async Task FlatDeviceDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("FLATDEVICE-DISCONNECTED");
        private async Task FlatDeviceLightToggledHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("FLATDEVICE-LIGHT-TOGGLED");
        private async Task FlatDeviceOpenedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("FLATDEVICE-OPENED");
        private async Task FlatDeviceClosedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("FLATDEVICE-CLOSED");
        private async Task FlatDeviceBrightnessChangedHandler(object sender, FlatDeviceBrightnessChangedEventArgs e) => await SubmitAndStoreEvent("FLATDEVICE-BRIGHTNESS-CHANGED", new
        {
            e.From,
            e.To,
        });

        public async void UpdateDeviceInfo(FlatDeviceInfo deviceInfo)
        {
            await SubmitEvent("FLATDEVICE-INFO-UPDATE", new FlatInfoResponse(flatDevice), WebSocketChannel.FlatdeviceInfoUpdate);
        }
    }
}
