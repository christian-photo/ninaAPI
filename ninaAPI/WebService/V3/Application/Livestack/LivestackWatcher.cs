#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Plugin.Interfaces;
using ninaAPI.WebService.Model;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Application.Livestack
{
    public class LivestackWatcher : EventWatcher, ISubscriber
    {
        private readonly IMessageBroker messageBroker;
        private readonly LiveStackHistory liveStackHistory;

        public LivestackWatcher(EventHistoryManager eventHistory, IMessageBroker messageBroker) : base(eventHistory)
        {
            this.messageBroker = messageBroker;
            liveStackHistory = new LiveStackHistory();
        }

        // TODO: Find possible values
        public static string LivestackStatus { get; private set; } = "stopped";

        public async Task OnMessageReceived(IMessage message)
        {
            switch (message.Topic)
            {
                case "Livestack_LivestackDockable_StackUpdateBroadcast":
                    await OnStackUpdateReceived(message);
                    break;

                case "Livestack_LivestackDockable_StatusBroadcast":
                    await OnStatusReceived(message);
                    break;

                default:
                    break;
            }
        }

        public async Task OnStatusReceived(IMessage message)
        {
            LivestackStatus = message.Content.ToString();
            await SubmitAndStoreEvent(WebSocketEvents.LIVESTACK_STATUS, new { Status = LivestackStatus });
        }

        public async Task OnStackUpdateReceived(IMessage message)
        {
            string filter = message.Content.GetType().GetProperty("Filter").GetValue(message.Content).ToString();
            string target = message.Content.GetType().GetProperty("Target").GetValue(message.Content).ToString();
            bool isMono = (bool)message.Content.GetType().GetProperty("IsMonochrome").GetValue(message.Content);
            int? stackCount = (int?)message.Content.GetType().GetProperty("StackCount").GetValue(message.Content);
            int? redImages = (int?)message.Content.GetType().GetProperty("RedStackCount").GetValue(message.Content);
            int? greenImages = (int?)message.Content.GetType().GetProperty("GreenStackCount").GetValue(message.Content);
            int? blueImages = (int?)message.Content.GetType().GetProperty("BlueStackCount").GetValue(message.Content);

            if (message.Content.GetType().GetProperty("Image").GetValue(message.Content) is BitmapSource image)
            {
                if (isMono)
                {
                    liveStackHistory.AddMono(filter, stackCount ?? -1, target, image);
                    await SubmitEvent(WebSocketEvents.LIVESTACK_STACK_UPDATED, new { Filter = filter, Target = target, IsMonochrome = isMono, StackCount = stackCount });
                }
                else
                {
                    liveStackHistory.AddColor(redImages ?? -1, greenImages ?? -1, blueImages ?? -1, filter, target, image);
                    await SubmitEvent(WebSocketEvents.LIVESTACK_STACK_UPDATED, new
                    {
                        Filter = filter,
                        Target = target,
                        IsMonochrome = isMono,
                        GreenStackCount = greenImages,
                        RedStackCount = redImages,
                        BlueStackCount = blueImages
                    });
                }
            }
        }

        public override void StartWatchers()
        {
            messageBroker.Subscribe("Livestack_LivestackDockable_StatusBroadcast", this);
            messageBroker.Unsubscribe("Livestack_LivestackDockable_StackUpdateBroadcast", this);
        }

        public override void StopWatchers()
        {
            messageBroker.Unsubscribe("Livestack_LivestackDockable_StatusBroadcast", this);
            messageBroker.Unsubscribe("Livestack_LivestackDockable_StackUpdateBroadcast", this);
            liveStackHistory.Dispose();
        }
    }
}