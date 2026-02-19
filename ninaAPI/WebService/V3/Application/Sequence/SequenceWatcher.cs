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
using NINA.Sequencer.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    public class SequenceWatcher : EventWatcher
    {
        private readonly ISequenceMediator sequence;

        public SequenceWatcher(EventHistoryManager historyManager, ISequenceMediator sequenceMediator) : base(historyManager)
        {
            Channel = WebSocketChannel.Sequence;
        }

        public override void StartWatchers()
        {
            Task.Run(async () =>
            {
                while (!sequence?.Initialized ?? true)
                {
                    await Task.Delay(50);
                }

                sequence.SequenceStarting += SequenceStarting;
                sequence.SequenceFinished += SequenceFinished;
            });
        }

        private async Task SequenceFinished(object arg1, EventArgs args)
        {
            await SubmitAndStoreEvent(WebSocketEvents.SEQUENCE_FINISHED);
        }

        private async Task SequenceStarting(object arg1, EventArgs args)
        {
            await SubmitAndStoreEvent(WebSocketEvents.SEQUENCE_STARTED);
        }

        public override void StopWatchers()
        {
            AdvancedAPI.Controls.Sequence.SequenceStarting -= SequenceStarting;
            AdvancedAPI.Controls.Sequence.SequenceFinished -= SequenceFinished;
        }
    }
}