#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public class ProcessWatcher : EventWatcher
    {
        public ProcessWatcher(EventHistoryManager eventHistory) : base(eventHistory)
        {
            this.Channel = WebSocketChannel.Process;
        }

        public override void StartWatchers()
        {
            ApiProcess.ProcessStarted += OnProcessStarted;
            ApiProcess.ProcessFinished += OnProcessFinished;
        }

        public override void StopWatchers()
        {
            ApiProcess.ProcessStarted -= OnProcessStarted;
            ApiProcess.ProcessFinished -= OnProcessFinished;
        }

        private async void OnProcessFinished(object sender, EventArgs e)
        {
            await SubmitEvent(WebSocketEvents.PROCESS_FINISHED,
                new { ProcessId = (sender as ApiProcess).ProcessId }
            );
        }

        private async void OnProcessStarted(object sender, EventArgs e)
        {
            await SubmitEvent(WebSocketEvents.PROCESS_STARTED,
                new { ProcessId = (sender as ApiProcess).ProcessId }
            );
        }
    }
}
