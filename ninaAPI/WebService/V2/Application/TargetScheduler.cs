#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Plugin.Interfaces;

namespace ninaAPI.WebService.V2
{
    public class TSWatcher : INinaWatcher, ISubscriber
    {
        public async Task OnMessageReceived(IMessage message)
        {
            string topic = message.Topic.Replace("TargetScheduler", "TS").ToUpper();
            Dictionary<string, object> data = new Dictionary<string, object>();
            if (message.Topic.Equals("TargetScheduler-WaitStart"))
            {
                data.Add("WaitStartTime", message.Content);
            }
            else if (message.Topic.Equals("TargetScheduler-NewTargetStart") || message.Topic.Equals("TargetScheduler-TargetStart"))
            {
                data.Add("TargetName", message.Content);
                data.Add("ProjectName", message.CustomHeaders["ProjectName"]);
                data.Add("Coordinates", message.CustomHeaders["Coordinates"]);
                data.Add("Rotation", message.CustomHeaders["Rotation"]);
            }
            await WebSocketV2.SendAndAddEvent(topic, message.SentAt.DateTime, data);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.MessageBroker.Subscribe("TargetScheduler-WaitStart", this);
            AdvancedAPI.Controls.MessageBroker.Subscribe("TargetScheduler-NewTargetStart", this);
            AdvancedAPI.Controls.MessageBroker.Subscribe("TargetScheduler-TargetStart", this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.MessageBroker.Unsubscribe("TargetScheduler-WaitStart", this);
            AdvancedAPI.Controls.MessageBroker.Unsubscribe("TargetScheduler-NewTargetStart", this);
            AdvancedAPI.Controls.MessageBroker.Unsubscribe("TargetScheduler-TargetStart", this);
        }
    }
}