#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.FilterWheel
{
    public class FilterWheelWatcher : EventWatcher, IFilterWheelConsumer
    {
        private class FilterChangedEvent
        {
            public FilterData From { get; set; }
            public FilterData To { get; set; }

            public static FilterChangedEvent FromEvent(FilterChangedEventArgs e)
            {
                return new FilterChangedEvent()
                {
                    From = FilterData.FromFilterShort(e.From),
                    To = FilterData.FromFilterShort(e.To),
                };
            }
        }

        private readonly IFilterWheelMediator filterWheel;

        public FilterWheelWatcher(EventHistoryManager eventHistory, IFilterWheelMediator filterWheel) : base(eventHistory)
        {
            Channel = Utility.Http.WebSocketChannel.Equipment;
            this.filterWheel = filterWheel;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            filterWheel.Connected += FilterWheelConnected;
            filterWheel.Disconnected += FilterWheelDisconnected;
            filterWheel.FilterChanged += FilterWheelFilterChanged;
            filterWheel.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            filterWheel.Connected -= FilterWheelConnected;
            filterWheel.Disconnected -= FilterWheelDisconnected;
            filterWheel.FilterChanged -= FilterWheelFilterChanged;
            filterWheel.RemoveConsumer(this);
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) { }

        private async Task FilterWheelConnected(object sender, EventArgs e) => await SubmitAndStoreEvent("FILTERWHEEL-CONNECTED");
        private async Task FilterWheelDisconnected(object sender, EventArgs e) => await SubmitAndStoreEvent("FILTERWHEEL-DISCONNECTED");
        private async Task FilterWheelFilterChanged(object sender, FilterChangedEventArgs e) => await SubmitAndStoreEvent("FILTERWHEEL-FILTER-CHANGED", FilterChangedEvent.FromEvent(e));
    }
}