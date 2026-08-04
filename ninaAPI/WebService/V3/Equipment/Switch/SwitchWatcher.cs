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
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Switch
{
    public sealed class SwitchWatcher : EventWatcher, ISwitchConsumer
    {
        private readonly ISwitchMediator @switch;

        public SwitchWatcher(EventHistoryManager history, ISwitchMediator @switch) : base(history)
        {
            this.@switch = @switch;
            Channel = WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            @switch.Connected += SwitchConnectedHandler;
            @switch.Disconnected += SwitchDisconnectedHandler;

            @switch.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            @switch.Connected -= SwitchConnectedHandler;
            @switch.Disconnected -= SwitchDisconnectedHandler;

            @switch.RemoveConsumer(this);
        }

        private async Task SwitchConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceConnected(Device.Switch));
        private async Task SwitchDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceDisconnected(Device.Switch));

        public async void UpdateDeviceInfo(SwitchInfo deviceInfo)
        {
            await SubmitEvent(WebSocketEvents.DeviceInfoUpdate(Device.Switch), new SwitchInfoResponse(@switch), WebSocketChannel.SwitchInfoUpdate);
        }
    }
}
