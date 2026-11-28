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
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Rotator
{
    public sealed class RotatorWatcher : EventWatcher, IRotatorConsumer
    {
        private readonly IRotatorMediator rotator;

        public RotatorWatcher(EventHistoryManager history, IRotatorMediator rotator) : base(history)
        {
            this.rotator = rotator;
            Channel = WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            rotator.Connected += RotatorConnectedHandler;
            rotator.Disconnected += RotatorDisconnectedHandler;
            rotator.Moved += RotatorMovedHandler;
            rotator.MovedMechanical += RotatorMovedMechanicalHandler;
            rotator.Synced += RotatorSyncedHandler;

            rotator.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            rotator.Connected -= RotatorConnectedHandler;
            rotator.Disconnected -= RotatorDisconnectedHandler;
            rotator.Moved -= RotatorMovedHandler;
            rotator.MovedMechanical -= RotatorMovedMechanicalHandler;
            rotator.Synced -= RotatorSyncedHandler;

            rotator.RemoveConsumer(this);
        }

        private async Task RotatorConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceConnected(Device.Rotator));
        private async Task RotatorDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceDisconnected(Device.Rotator));
        private async Task RotatorMovedHandler(object sender, RotatorEventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.ROTATOR_MOVED,
            new
            {
                From = e.From,
                To = e.To,
                Mechanical = false
            });
        private async Task RotatorMovedMechanicalHandler(object sender, RotatorEventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.ROTATOR_MOVED,
            new
            {
                From = e.From,
                To = e.To,
                Mechanical = true
            });
        private async void RotatorSyncedHandler(object sender, RotatorEventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.ROTATOR_SYNCED,
            new
            {
                From = e.From,
                To = e.To
            });


        public async void UpdateDeviceInfo(RotatorInfo deviceInfo)
        {
            await SubmitEvent(WebSocketEvents.DeviceInfoUpdate(Device.Rotator), new RotatorInfoResponse(rotator), WebSocketChannel.RotatorInfoUpdate);
        }
    }
}
