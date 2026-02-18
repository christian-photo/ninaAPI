#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
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
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.SequenceItems
{
    [ExportMetadata("Name", "Send WebSocket Message")]
    [ExportMetadata("Description", "Sends a text message to the WebSocket")]
    [ExportMetadata("Icon", "ScriptSVG")]
    [ExportMetadata("Category", "Advanced API")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]
    public partial class SendMessageInstruction : SequenceItem, IValidatable
    {
        [ImportingConstructor]
        public SendMessageInstruction() { }

        private SendMessageInstruction(SendMessageInstruction other) : this()
        {
            CopyMetaData(other);
        }

        [IsExpression(DefaultString = "Test event")]
        public partial string Message { get; set; }


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

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            await WebSocketV2.SendEvent(new HttpResponse() { Response = Message, Type = HttpResponse.TypeSocket }).WaitAsync(token);
            await (AdvancedAPI.V3 as V3Api).GetEventWebSocket().SendEvent(new WebSocketEvent()
            {
                Channel = WebSocketChannel.Sequence,
                Event = WebSocketEvents.SEQUENCE_CUSTOM_EVENT,
                Data = Message,
            }).WaitAsync(token);
        }

        public bool Validate()
        {
            List<string> i = new List<string>();
            if (!WebSocketV2.IsAvailable && (AdvancedAPI.V3 is null || (AdvancedAPI.V3 as V3Api).GetEventWebSocket() is null))
            {
                i.Add("WebSocket is not available");
            }
            Expression.ValidateExpressions(i, MessageExpression);

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged()
        {
            Validate();
        }

        public override string ToString()
        {
            return $"Category: {Category}, Item: {Name}, Message: {Message}";
        }
    }
}