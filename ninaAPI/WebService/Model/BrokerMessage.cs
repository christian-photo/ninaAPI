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
using NINA.Plugin.Interfaces;

namespace ninaAPI.WebService.Model
{
    public class NINAMessage(Guid correlatedGuid, string topic, object content) : IMessage
    {
        public Guid SenderId => Guid.Parse(AdvancedAPI.PluginId);

        public string Sender => nameof(ninaAPI);

        public DateTimeOffset SentAt => DateTimeOffset.Now;

        public Guid MessageId => Guid.NewGuid();

        public DateTimeOffset? Expiration => null;

        public Guid? CorrelationId => correlatedGuid;

        public int Version => 1;

        public IDictionary<string, object> CustomHeaders => new Dictionary<string, object>();

        public string Topic => topic;

        public object Content => content;
    }
}