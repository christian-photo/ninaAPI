#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Specialized;
using System.Linq;
using NINA.Profile.Interfaces;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Application.Profile
{
    public class ProfileWatcher : EventWatcher
    {
        private readonly IProfileService profileService;

        public ProfileWatcher(EventHistoryManager eventHistory, IProfileService profileService) : base(eventHistory)
        {
            this.profileService = profileService;

            Channel = WebSocketChannel.General;
        }

        public override void StartWatchers()
        {
            profileService.ProfileChanged += ProfileChanged;
            profileService.Profiles.CollectionChanged += ProfileListChanged;
        }

        public override void StopWatchers()
        {
            profileService.ProfileChanged -= ProfileChanged;
            profileService.Profiles.CollectionChanged -= ProfileListChanged;
        }

        private async void ProfileListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                await SubmitAndStoreEvent(WebSocketEvents.PROFILE_ADDED, e.NewItems[0]);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                await SubmitAndStoreEvent(WebSocketEvents.PROFILE_REMOVED, e.OldItems[0]);
            }
        }

        private async void ProfileChanged(object sender, EventArgs e)
        {
            await SubmitAndStoreEvent(WebSocketEvents.PROFILE_CHANGED, profileService.Profiles.First(x => x.Id == profileService.ActiveProfile.Id));
        }
    }
}
