#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using Swan.Validators;

namespace ninaAPI.WebService.V3.Model
{
    public class PlatesolveConfig
    {
        [Range(1, int.MaxValue)]
        public int? Attempts { get; set; }

        [Range(1, int.MaxValue)]
        public int? DownSampleFactor { get; set; }

        public bool? BlindFailoverEnabled { get; set; }

        [Range(0, double.MaxValue)]
        public double? SearchRadius { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxObjects { get; set; }

        [Range(1, short.MaxValue)]
        public short? Binning { get; set; }

        [Range(0, int.MaxValue)]
        public int? Regions { get; set; }

        public RawConverterEnum? RawConverter { get; set; }

        public void UpdateWithProfile(IProfile profile)
        {
            Attempts = this.Attempts ?? profile.PlateSolveSettings.NumberOfAttempts;
            DownSampleFactor = this.DownSampleFactor ?? profile.PlateSolveSettings.DownSampleFactor;
            BlindFailoverEnabled = this.BlindFailoverEnabled ?? profile.PlateSolveSettings.BlindFailoverEnabled;
            SearchRadius = this.SearchRadius ?? profile.PlateSolveSettings.SearchRadius;
            MaxObjects = this.MaxObjects ?? profile.PlateSolveSettings.MaxObjects;
            Binning = this.Binning ?? profile.PlateSolveSettings.Binning;
            Regions = this.Regions ?? profile.PlateSolveSettings.Regions;
            RawConverter = this.RawConverter ?? profile.CameraSettings.RawConverter;
        }
    }
}