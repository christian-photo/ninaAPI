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
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile.Interfaces;

namespace ninaAPI.WebService.V3.Equipment.FilterWheel
{
    public class FilterWheelInfoResponse
    {
        public bool Connected { get; set; }
        public string Description { get; set; }
        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public string DriverVersion { get; set; }
        public string DriverInfo { get; set; }
        public bool IsMoving { get; set; }
        public string Name { get; set; }
        public FilterData SelectedFilter { get; set; }
        public List<FilterData> AvailableFilters { get; set; }
        public IList<string> SupportedActions { get; set; }

        public static FilterWheelInfoResponse FromInfo(FilterWheelInfo info, IProfile profile)
        {
            bool hasFilters = profile.FilterWheelSettings.FilterWheelFilters.Count > 0;
            return new FilterWheelInfoResponse()
            {
                Name = info.Name,
                DisplayName = info.DisplayName,
                DriverVersion = info.DriverVersion,
                DriverInfo = info.DriverInfo,
                Connected = info.Connected,
                Description = info.Description,
                DeviceId = info.DeviceId,
                IsMoving = info.IsMoving,
                SelectedFilter = info.SelectedFilter is not null ? FilterData.FromFilter(info.SelectedFilter) : null,
                AvailableFilters = hasFilters ? profile.FilterWheelSettings.FilterWheelFilters.Select(f => FilterData.FromFilterShort(f))?.ToList() : null,
                SupportedActions = info.SupportedActions,
            };
        }

        public FilterWheelInfoResponse() { }
    }
}