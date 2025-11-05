#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Net;
using EmbedIO;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;

namespace ninaAPI.WebService.V3.Equipment.FilterWheel
{
    public class FilterData
    {
        public string Name { get; set; }
        public int? FocusOffset { get; set; }
        public short Position { get; set; }
        public double? AutoFocusExposureTime { get; set; }
        public bool? AutoFocusFilter { get; set; }
        public BinningMode AutoFocusBinning { get; set; }
        public int? AutoFocusGain { get; set; }
        public int? AutoFocusOffset { get; set; }

        public static FilterData FromFilter(FilterInfo filter)
        {
            return new FilterData()
            {
                Name = filter.Name,
                FocusOffset = filter.FocusOffset,
                Position = filter.Position,
                AutoFocusExposureTime = filter.AutoFocusExposureTime,
                AutoFocusFilter = filter.AutoFocusFilter,
                AutoFocusBinning = filter.AutoFocusBinning,
                AutoFocusGain = filter.AutoFocusGain,
                AutoFocusOffset = filter.AutoFocusOffset,
            };
        }

        public static FilterData FromFilterShort(FilterInfo filter)
        {
            return new FilterData()
            {
                Name = filter.Name,
                Position = filter.Position,
                FocusOffset = null,
                AutoFocusBinning = null,
                AutoFocusExposureTime = null,
                AutoFocusFilter = null,
                AutoFocusGain = null,
                AutoFocusOffset = null,
            };
        }

        public static FilterData FromFilterPosition(short position, IProfile profile)
        {
            return FromFilter(ToFilter(position, profile));
        }

        public FilterInfo ToFilter(IProfile profile)
        {
            return ToFilter(Position, profile);
        }

        public static FilterInfo ToFilter(short position, IProfile profile)
        {
            if (profile.FilterWheelSettings.FilterWheelFilters.Count <= position)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Filter not available");
            }
            return profile.FilterWheelSettings.FilterWheelFilters[position];
        }
    }
}