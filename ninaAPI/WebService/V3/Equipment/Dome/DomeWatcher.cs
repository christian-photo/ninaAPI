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
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Dome
{
    public class DomeWatcher : EventWatcher, IDomeConsumer
    {
        private readonly IDomeMediator dome;
        private readonly IDomeFollower follower;

        public DomeWatcher(EventHistoryManager eventHistory, IDomeMediator dome, IDomeFollower follower) : base(eventHistory)
        {
            this.dome = dome;
            this.follower = follower;

            Channel = WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            dome.Connected += DomeConnected;
            dome.Disconnected += DomeDisconnected;
            dome.Closed += DomeClosed;
            dome.Opened += DomeOpened;
            dome.Homed += DomeHomed;
            dome.Parked += DomeParked;
            dome.Slewed += DomeSlewed;
            dome.Synced += DomeSynced;

            dome.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            dome.Connected -= DomeConnected;
            dome.Disconnected -= DomeDisconnected;
            dome.Closed -= DomeClosed;
            dome.Opened -= DomeOpened;
            dome.Homed -= DomeHomed;
            dome.Parked -= DomeParked;
            dome.Slewed -= DomeSlewed;
            dome.Synced -= DomeSynced;

            dome.RemoveConsumer(this);
        }

        private async Task DomeConnected(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-CONNECTED");
        private async Task DomeDisconnected(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-DISCONNECTED");
        private async Task DomeClosed(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-SHUTTER-CLOSED");
        private async Task DomeOpened(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-SHUTTER-OPENED");
        private async Task DomeHomed(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-HOMED");
        private async Task DomeParked(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-PARKED");
        private async Task DomeSlewed(object sender, DomeEventArgs e) => await SubmitAndStoreEvent("DOME-SLEWED", new
        {
            e.From,
            e.To
        });
        private async void DomeSynced(object sender, EventArgs e) => await SubmitAndStoreEvent("DOME-SYNCED");

        public async void UpdateDeviceInfo(DomeInfo deviceInfo)
        {
            await SubmitEvent("DOME-INFO-UPDATE", new DomeInfoResponse(dome, follower), WebSocketChannel.DomeInfoUpdate);
        }
    }
}
