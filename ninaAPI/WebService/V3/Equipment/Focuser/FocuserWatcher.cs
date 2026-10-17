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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;
using OxyPlot;

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    public sealed class FocuserWatcher : EventWatcher, IFocuserConsumer
    {
        private readonly IFocuserMediator focuser;

        public FocuserWatcher(EventHistoryManager eventHistory, IFocuserMediator focuser) : base(eventHistory)
        {
            Channel = WebSocketChannel.Equipment;
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

        private async Task FocuserConnected(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceConnected(Device.Focuser));
        private async Task FocuserDisconnected(object sender, EventArgs e) => await SubmitAndStoreEvent(WebSocketEvents.DeviceDisconnected(Device.Focuser));
        public async void UpdateUserFocused(FocuserInfo info) => await SubmitEvent(WebSocketEvents.FOCUSER_USER_FOCUSED, onChannel: WebSocketChannel.Autofocus);
        public async void NewAutoFocusPoint(DataPoint dataPoint) => await SubmitEvent(WebSocketEvents.FOCUSER_NEW_AF_POINT, dataPoint, WebSocketChannel.Autofocus);

        public async void UpdateEndAutoFocusRun(AutoFocusInfo info)
        {
            IsAutoFocusRunning = false;
            await SubmitAndStoreEvent(WebSocketEvents.FOCUSER_AF_ENDED, info, WebSocketChannel.Autofocus);
        }
        public async void AutoFocusRunStarting()
        {
            IsAutoFocusRunning = true;
            await SubmitAndStoreEvent(WebSocketEvents.FOCUSER_AF_STARTED, onChannel: WebSocketChannel.Autofocus);
        }

        public async void UpdateDeviceInfo(FocuserInfo deviceInfo)
        {
            await SubmitEvent(WebSocketEvents.DeviceInfoUpdate(Device.Focuser), new FocuserInfoResponse(focuser), WebSocketChannel.FocuserInfoUpdate);
        }
    }
}
