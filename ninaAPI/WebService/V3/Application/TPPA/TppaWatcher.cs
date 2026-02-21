#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Plugin.Interfaces;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Application.TPPA
{
    public class TppaWatcher : EventWatcher, ISubscriber
    {
        private readonly IMessageBroker messageBroker;

        public TppaWatcher(EventHistoryManager eventHistory, IMessageBroker messageBroker) : base(eventHistory)
        {
            this.messageBroker = messageBroker;
        }

        public async Task OnMessageReceived(IMessage message)
        {
            if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_AlignmentError" && message.Version == 1)
            {
                await SubmitEvent(WebSocketEvents.TPPA_ALIGNMENT_ERROR, message.Content);
            }
            else if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_Progress")
            {
                ApplicationStatus status = (ApplicationStatus)message.Content;

                await SubmitEvent(WebSocketEvents.TPPA_PROGRESS_UPDATE, new
                {
                    Status = status.Status,
                    Progress = status.Progress / status.MaxProgress,
                });
            }
        }

        public override void StartWatchers()
        {
            messageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_AlignmentError", this);
            messageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_Progress", this);
        }

        public override void StopWatchers()
        {
            messageBroker.Unsubscribe("PolarAlignmentPlugin_PolarAlignment_AlignmentError", this);
            messageBroker.Unsubscribe("PolarAlignmentPlugin_PolarAlignment_Progress", this);
        }
    }
}