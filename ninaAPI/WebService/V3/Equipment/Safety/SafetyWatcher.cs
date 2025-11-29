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
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Safety
{
    public class SafetyWatcher : EventWatcher, ISafetyMonitorConsumer
    {
        private readonly ISafetyMonitorMediator safety;

        public SafetyWatcher(EventHistoryManager history, ISafetyMonitorMediator safety) : base(history)
        {
            this.safety = safety;
            Channel = Utility.Http.WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            safety.Connected += SafetyConnectedHandler;
            safety.Disconnected += SafetyDisconnectedHandler;
            safety.IsSafeChanged += SafetyChangedHandler;
        }

        public override void StopWatchers()
        {
            safety.Connected -= SafetyConnectedHandler;
            safety.Disconnected -= SafetyDisconnectedHandler;
            safety.IsSafeChanged -= SafetyChangedHandler;
        }

        private async Task SafetyConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("SAFETY-CONNECTED");
        private async Task SafetyDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("SAFETY-DISCONNECTED");
        private async void SafetyChangedHandler(object sender, IsSafeEventArgs e) => await SubmitAndStoreEvent("SAFETY-CHANGED",
            new
            {
                IsSafe = e.IsSafe
            });

        public void UpdateDeviceInfo(SafetyMonitorInfo deviceInfo)
        {

        }
    }
}
