#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Threading.Tasks;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public abstract class EventWatcher : INinaWatcher
    {
        private readonly IEventSocket eventSocket;
        public EventWatcher(IEventSocket eventSocket)
        {
            this.eventSocket = eventSocket;
        }

        public async Task SubmitEvent(string eventName, object data = null)
        {
            await SubmitEvent(new WebSocketEvent()
            {
                Event = eventName,
                Channel = WebSocketChannel.Equipment,
                Data = data
            });
        }

        public async Task SubmitEvent(WebSocketEvent e)
        {
            if (eventSocket.IsActive)
            {
                eventSocket.EventHistoryManager.AddEventToHistory(e);
                await eventSocket.SendEvent(e);
            }
        }

        public abstract void StartWatchers();
        public abstract void StopWatchers();
    }
}