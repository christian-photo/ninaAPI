#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace ninaAPI.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NINA.Plugin.Interfaces;
    using ninaAPI;

    public class Communicator : ISubscriber, IDisposable
    {
        public Communicator()
        {
            AdvancedAPI.Controls.MessageBroker.Subscribe("AdvancedAPI.RequestPort", this);
        }

        public void Dispose()
        {
            AdvancedAPI.Controls.MessageBroker.Unsubscribe("AdvancedAPI.RequestPort", this);
        }

        public Task OnMessageReceived(IMessage message)
        {
            return AdvancedAPI.Controls.MessageBroker.Publish(new PortRequestMessage(AdvancedAPI.GetCachedPort(), message.CorrelationId.Value));
        }
    }

    public class PortRequestMessage(int port, Guid correlatedGuid) : IMessage
    {
        public Guid SenderId => Guid.Parse(AdvancedAPI.PluginId);

        public string Sender => nameof(ninaAPI);

        public DateTimeOffset SentAt => DateTime.UtcNow;

        public Guid MessageId => Guid.NewGuid();

        public DateTimeOffset? Expiration => null;

        public Guid? CorrelationId => correlatedGuid;

        public int Version => 1;

        public IDictionary<string, object> CustomHeaders => new Dictionary<string, object>();

        public string Topic => "AdvancedAPI.Port";

        public object Content => port;
    }
}