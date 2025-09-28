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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Websocket.Event;
using OxyPlot;

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    public class FocuserWatcher : EventWatcher, IFocuserConsumer
    {
        private readonly IFocuserMediator focuser;

        public FocuserWatcher(IEventSocket eventSocket, IFocuserMediator focuser) : base(eventSocket)
        {
            Channel = WebSocketChannel.Autofocus;
            this.focuser = focuser;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            focuser.RegisterConsumer(this);
            focuser.Connected += FocuserConnected;
            focuser.Disconnected += FocuserDisconnected;
        }

        public override void StopWatchers()
        {
            focuser.RemoveConsumer(this);
            focuser.Connected -= FocuserConnected;
            focuser.Disconnected -= FocuserDisconnected;
        }

        public static bool IsAutoFocusRunning { get; private set; } = false;

        private async Task FocuserConnected(object sender, EventArgs e) => await SubmitAndStoreEvent("FOCUSER-CONNECTED", onChannel: WebSocketChannel.Equipment);
        private async Task FocuserDisconnected(object sender, EventArgs e) => await SubmitAndStoreEvent("FOCUSER-DISCONNECTED", onChannel: WebSocketChannel.Equipment);
        public async void UpdateUserFocused(FocuserInfo info) => await SubmitEvent("USER-FOCUSED", info, onChannel: WebSocketChannel.Equipment);
        public async void NewAutoFocusPoint(DataPoint dataPoint) => await SubmitEvent("AUTOFOCUS-POINT", dataPoint);

        public async void UpdateEndAutoFocusRun(AutoFocusInfo info)
        {
            IsAutoFocusRunning = false;
            await SubmitAndStoreEvent("AUTOFOCUS-ENDED", info);
        }
        public async void AutoFocusRunStarting()
        {
            IsAutoFocusRunning = true;
            await SubmitAndStoreEvent("AUTOFOCUS-STARTED");
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) { }
    }
}