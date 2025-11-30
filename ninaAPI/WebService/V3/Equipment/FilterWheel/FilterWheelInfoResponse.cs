#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Collections.Generic;
using System.Linq;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Equipment.FilterWheel
{
    public class FilterWheelInfoResponse : FilterWheelInfo
    {
        public FilterData CurrentFilter { get; set; }
        public List<FilterData> AvailableFilters { get; set; }

        public FilterWheelInfoResponse(IFilterWheelMediator filterwheel, IProfile profile)
        {
            var info = filterwheel.GetInfo();
            bool hasFilters = profile.FilterWheelSettings.FilterWheelFilters.Count > 0;

            CoreUtility.CopyProperties(info, this);
            CurrentFilter = info.SelectedFilter is not null ? FilterData.FromFilter(info.SelectedFilter) : null;
            AvailableFilters = hasFilters ? profile.FilterWheelSettings.FilterWheelFilters.Select(f => FilterData.FromFilterShort(f))?.ToList() : null;

            SelectedFilter = null;
        }
    }
}
