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
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public class ClientConfiguration
    {
        public ChannelSubscriptionManager SubscriptionManager { get; }

        public ClientConfiguration()
        {
            SubscriptionManager = new ChannelSubscriptionManager([.. Enum.GetValues<WebSocketChannel>()]); // Subscribe to all channels by default

            SubscriptionManager.Unsubscribe(WebSocketChannel.CameraInfoUpdate); // except the info channels because these might have a lot of traffic
            SubscriptionManager.Unsubscribe(WebSocketChannel.DomeInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.FilterwheelInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.FlatdeviceInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.FocuserInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.GuiderInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.MountInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.RotatorInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.SafetyInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.SwitchInfoUpdate);
            SubscriptionManager.Unsubscribe(WebSocketChannel.WeatherInfoUpdate);
        }
    }

    public class ChannelSubscriptionManager
    {
        private ThreadSafeList<WebSocketChannel> SubscribedChannels;

        public ChannelSubscriptionManager(List<WebSocketChannel> channels)
        {
            SubscribedChannels = new ThreadSafeList<WebSocketChannel>(channels);
        }

        public void Unsubscribe(WebSocketChannel channel)
        {
            SubscribedChannels.Remove(channel);
        }

        public void Subscribe(WebSocketChannel channel)
        {
            SubscribedChannels.Add(channel);
        }

        public bool IsSubscribed(WebSocketChannel channel)
        {
            return SubscribedChannels.Contains(channel);
        }

        public List<WebSocketChannel> GetSubscribedChannels()
        {
            return SubscribedChannels.ToList();
        }
    }
}
