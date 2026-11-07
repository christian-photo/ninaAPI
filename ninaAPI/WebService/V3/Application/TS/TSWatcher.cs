#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Threading.Tasks;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Application.TS
{
    public class TSWatcher : EventWatcher, ISubscriber
    {
        private readonly IMessageBroker messageBroker;

        private const string WAIT_START = "TargetScheduler-WaitStart";
        private const string NEW_TARGET_START = "TargetScheduler-NewTargetStart";
        private const string TARGET_START = "TargetScheduler-TargetStart";
        private const string CONTAINER_STOPPED = "TargetScheduler-ContainerStopped";
        private const string TARGET_COMPLETE = "TargetScheduler-TargetComplete";

        public TSWatcher(EventHistoryManager eventHistory, IMessageBroker messageBroker) : base(eventHistory)
        {
            this.messageBroker = messageBroker;
            Channel = WebSocketChannel.TargetScheduler;
        }

        public async Task OnMessageReceived(IMessage message)
        {
            var eventName = message.Topic switch
            {
                WAIT_START => WebSocketEvents.TS_WAITSTART,
                NEW_TARGET_START => WebSocketEvents.TS_NEWTARGETSTART,
                TARGET_START => WebSocketEvents.TS_TARGETSTART,
                CONTAINER_STOPPED => WebSocketEvents.TS_STOP,
                TARGET_COMPLETE => WebSocketEvents.TS_COMPLETE,
                _ => null
            };

            if (eventName is null)
            {
                return;
            }

            await SubmitAndStoreEvent(eventName, message.CustomHeaders);
        }

        public override void StartWatchers()
        {
            messageBroker.Subscribe(WAIT_START, this);
            messageBroker.Subscribe(NEW_TARGET_START, this);
            messageBroker.Subscribe(TARGET_START, this);
            messageBroker.Subscribe(CONTAINER_STOPPED, this);
            messageBroker.Subscribe(TARGET_COMPLETE, this);
        }

        public override void StopWatchers()
        {
            messageBroker.Unsubscribe(WAIT_START, this);
            messageBroker.Unsubscribe(NEW_TARGET_START, this);
            messageBroker.Unsubscribe(TARGET_START, this);
            messageBroker.Unsubscribe(CONTAINER_STOPPED, this);
            messageBroker.Unsubscribe(TARGET_COMPLETE, this);
        }
    }
}