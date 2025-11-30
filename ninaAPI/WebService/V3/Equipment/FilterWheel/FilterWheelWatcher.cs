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
using NINA.Profile.Interfaces;
using ninaAPI.Utility.Http;
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
        private readonly IProfileService profileService;

        public FilterWheelWatcher(EventHistoryManager eventHistory, IFilterWheelMediator filterWheel, IProfileService profileService) : base(eventHistory)
        {
            this.filterWheel = filterWheel;
            this.profileService = profileService;

            Channel = WebSocketChannel.Equipment;
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

        private async Task FilterWheelConnected(object sender, EventArgs e) => await SubmitAndStoreEvent("FILTERWHEEL-CONNECTED");
        private async Task FilterWheelDisconnected(object sender, EventArgs e) => await SubmitAndStoreEvent("FILTERWHEEL-DISCONNECTED");
        private async Task FilterWheelFilterChanged(object sender, FilterChangedEventArgs e) => await SubmitAndStoreEvent("FILTERWHEEL-FILTER-CHANGED", FilterChangedEvent.FromEvent(e));

        public async void UpdateDeviceInfo(FilterWheelInfo deviceInfo)
        {
            await SubmitEvent("FILTERWHEEL-INFO-UPDATE", new FilterWheelInfoResponse(filterWheel, profileService.ActiveProfile), WebSocketChannel.FilterwheelInfoUpdate);
        }
    }
}
