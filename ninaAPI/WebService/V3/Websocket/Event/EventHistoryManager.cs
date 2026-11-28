#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Collections.Generic;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public class EventHistoryManager
    {
        private readonly ThreadSafeList<WebSocketHistoryEvent> EventHistory = new();

        public void AddEventToHistory(WebSocketEvent e)
        {
            EventHistory.Add(WebSocketHistoryEvent.FromEvent(e));
        }

        public List<WebSocketHistoryEvent> GetEventHistory()
        {
            return EventHistory.ToList();
        }

        public List<WebSocketHistoryEvent> GetEventHistoryPage(int page, int pageSize)
        {
            return new Pager<WebSocketHistoryEvent>(EventHistory.ToList()).GetPage(page, pageSize);
        }
    }
}