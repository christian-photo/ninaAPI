#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;

namespace ninaAPI.Utility.Http
{
    public class WebSocketEvent
    {
        public string Event { get; set; }
        public WebSocketChannel Channel { get; set; }
        public object Data { get; set; }
    }

    public class WebSocketHistoryEvent : WebSocketEvent
    {
        public DateTime Timestamp { get; set; }

        public static WebSocketHistoryEvent FromEvent(WebSocketEvent e)
        {
            return new WebSocketHistoryEvent()
            {
                Event = e.Event,
                Channel = e.Channel,
                Data = e.Data,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public enum WebSocketChannel
    {
        Equipment,
        Capture,
        Livestack,
        Autofocus,
        Process
    }
}