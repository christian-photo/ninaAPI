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
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using ninaAPI.WebService.V2;

namespace ninaAPI.SequenceItems
{
    [ExportMetadata("Name", "Send Error to WebSocket")]
    [ExportMetadata("Description", "Sends an error message to the websocket if a sequence item failed")]
    [ExportMetadata("Icon", "ScriptSVG")]
    [ExportMetadata("Category", "Advanced API")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendErrorTrigger : SequenceTrigger, IValidatable
    {
        private ISequenceRootContainer root;
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

        [ImportingConstructor]
        public SendErrorTrigger() { }

        public override object Clone()
        {
            return new SendErrorTrigger()
            {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        public bool Validate()
        {
            List<string> i = new List<string>();
            if (!WebSocketV2.IsAvailable)
            {
                i.Add("WebSocket is not available");
            }

            Issues = i;
            return i.Count == 0;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            return false;
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public override void SequenceBlockInitialize()
        {
            root = ((SequenceContainer)this.Parent).GetRootContainer(this.Parent);
            root.FailureEvent += Root_FailureEvent;

            base.SequenceBlockInitialize();
        }

        private async Task Root_FailureEvent(object arg1, SequenceEntityFailureEventArgs args)
        {
            Logger.Debug($"Detected failure event, sending to websocket: {args.Entity.Name}, {args.Exception.Message}");
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("Entity", args.Entity.Name);
            data.Add("Error", args.Exception.Message);
            await WebSocketV2.SendAndAddEvent("SEQUENCE-ENTITY-FAILED", data);
        }

        public override void SequenceBlockTeardown()
        {
            if (root != null)
            {
                root.FailureEvent -= Root_FailureEvent;
            }

            base.SequenceBlockTeardown();
        }
    }
}