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
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Mount
{
    public class MountWatcher : EventWatcher, ITelescopeConsumer
    {
        private readonly ITelescopeMediator mount;

        public MountWatcher(EventHistoryManager history, ITelescopeMediator mount) : base(history)
        {
            this.mount = mount;
            Channel = WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            mount.Connected += MountConnectedHandler;
            mount.Disconnected += MountDisconnectedHandler;
            mount.AfterMeridianFlip += MountAfterMFHandler;
            mount.BeforeMeridianFlip += MountBeforeMFHandler;
            mount.Homed += MountHomedHandler;
            mount.Parked += MountParkedHandler;
            mount.Slewed += MountSlewedHandler;
            mount.Unparked += MountUnparkedHandler;

            mount.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            mount.Connected -= MountConnectedHandler;
            mount.Disconnected -= MountDisconnectedHandler;
            mount.AfterMeridianFlip -= MountAfterMFHandler;
            mount.BeforeMeridianFlip -= MountBeforeMFHandler;
            mount.Homed -= MountHomedHandler;
            mount.Parked -= MountParkedHandler;
            mount.Slewed -= MountSlewedHandler;
            mount.Unparked -= MountUnparkedHandler;

            mount.RemoveConsumer(this);
        }

        private async Task MountConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("MOUNT-CONNECTED");
        private async Task MountDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("MOUNT-DISCONNECTED");
        private async Task MountAfterMFHandler(object sender, AfterMeridianFlipEventArgs e) => await SubmitAndStoreEvent("MOUNT-FLIP-FINISHED",
            new
            {
                TargetCoordinates = e.Target,
                Success = e.Success
            });
        private async Task MountBeforeMFHandler(object sender, BeforeMeridianFlipEventArgs e) => await SubmitAndStoreEvent("MOUNT-FLIP-STARTED",
            new
            {
                TargetCoordinates = e.Target
            });
        private async Task MountHomedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("MOUNT-HOMED");
        private async Task MountParkedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("MOUNT-PARKED");
        private async Task MountSlewedHandler(object sender, MountSlewedEventArgs e) => await SubmitAndStoreEvent("MOUNT-SLEWED",
            new
            {
                From = e.From,
                To = e.To
            });
        private async Task MountUnparkedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("MOUNT-UNPARKED");

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo)
        {

        }
    }
}
