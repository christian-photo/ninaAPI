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
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Switch
{
    public class SwitchWatcher : EventWatcher, ISwitchConsumer
    {
        private readonly ISwitchMediator @switch;

        public SwitchWatcher(EventHistoryManager history, ISwitchMediator @switch) : base(history)
        {
            this.@switch = @switch;
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

        private async Task SwitchConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("SWITCH-CONNECTED");
        private async Task SwitchDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("SWITCH-DISCONNECTED");

        public void UpdateDeviceInfo(SwitchInfo deviceInfo)
        {

        }
    }
}
