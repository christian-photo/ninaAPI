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
        public WebSocketChannel Channel { get; protected set; } = WebSocketChannel.Equipment;

        private IEventSocket? eventSocket;
        private readonly EventHistoryManager eventHistory;

        public bool IsActive => eventSocket != null && eventSocket.HasConnections;

        public EventWatcher(EventHistoryManager eventHistory)
        {
            this.eventHistory = eventHistory;
        }

        /// <summary>
        /// Initializes the watcher with the given event socket. Before the watcher is initialized, it will only save the events to the history.
        /// </summary>
        /// <param name="eventSocket"></param>
        public void Initialize(IEventSocket eventSocket)
        {
            if (!IsActive)
            {
                this.eventSocket = eventSocket;
            }
        }

        /// <summary>
        /// Sends an event to the websocket and stores it in the history
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="data">Additional data to send with the event</param>
        /// <param name="onChannel">The channel to send the event to</param>
        /// <returns>True, if the event was actually sent to the websocket, false otherwise</returns>
        public async Task<bool> SubmitAndStoreEvent(string eventName, object data = null, WebSocketChannel? onChannel = null)
        {
            return await SubmitAndStoreEvent(new WebSocketEvent()
            {
                Event = eventName,
                Channel = onChannel ?? Channel,
                Data = data
            });
        }

        /// <summary>
        /// Sends an event to the websocket and stores it in the history
        /// </summary>
        /// <param name="e">The websocket event</param>
        /// <returns>True, if the event was actually sent to the websocket, false otherwise</returns>
        public async Task<bool> SubmitAndStoreEvent(WebSocketEvent e)
        {
            eventHistory.AddEventToHistory(e);
            return await SubmitEvent(e);
        }

        /// <summary>
        /// Sends an event to the websocket
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="data">Additional data to send with the event</param>
        /// <param name="onChannel">The channel to send the event to</param>
        /// <returns>True, if the event was actually sent to the websocket, false otherwise</returns>
        public async Task<bool> SubmitEvent(string eventName, object data = null, WebSocketChannel? onChannel = null)
        {
            return await SubmitEvent(new WebSocketEvent()
            {
                Event = eventName,
                Channel = onChannel ?? Channel,
                Data = data
            });
        }

        /// <summary>
        /// Sends an event to the websocket
        /// </summary>
        /// <param name="e">The websocket event</param>
        /// <returns>True, if the event was actually sent to the websocket, false otherwise</returns>
        public async Task<bool> SubmitEvent(WebSocketEvent e)
        {
            if (eventSocket?.HasConnections ?? false)
            {
                await eventSocket.SendEvent(e);
                return true;
            }
            return false;
        }

        public abstract void StartWatchers();
        public abstract void StopWatchers();
    }
}
