#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.SequenceItems
{
    [ExportMetadata("Name", "Send WebSocket Event")]
    [ExportMetadata("Description", "Sends a text message as an event to the WebSocket")]
    [ExportMetadata("Icon", "ScriptSVG")]
    [ExportMetadata("Category", "Advanced API")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendEventInstruction : SequenceItem, IValidatable
    {
        private IList<string> issues = new List<string>();

        public IList<string> Issues
        {
            get => issues;
            set
            {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private string message = "Test event";

        [JsonProperty]
        public string Message
        {
            get => message?.Trim();
            set
            {
                message = value?.Trim();
                RaisePropertyChanged();
            }
        }

        [ImportingConstructor]
        public SendEventInstruction() { }

        public override object Clone()
        {
            return new SendEventInstruction()
            {
                Message = Message,
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            await WebSocketV2.SendEvent(new HttpResponse() { Response = string.IsNullOrEmpty(Message) ? "Test event" : Message, Type = HttpResponse.TypeSocket }).WaitAsync(token);
            await (AdvancedAPI.V3 as V3Api).GetEventWebSocket().SendEvent(new WebSocketEvent()
            {
                Channel = WebSocketChannel.Sequence,
                Event = WebSocketEvents.SEQUENCE_CUSTOM_EVENT,
                Data = string.IsNullOrEmpty(Message) ? "Test event" : Message,
            }).WaitAsync(token);
        }

        public bool Validate()
        {
            List<string> i = new List<string>();
            if (!WebSocketV2.IsAvailable && (AdvancedAPI.V3 is null || (AdvancedAPI.V3 as V3Api).GetEventWebSocket() is null))
            {
                i.Add("WebSocket is not available");
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}