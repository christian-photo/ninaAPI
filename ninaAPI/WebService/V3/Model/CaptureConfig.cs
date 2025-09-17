#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Core.Model.Equipment;

namespace ninaAPI.WebService.V3.Model
{
    public class CaptureConfig
    {
        public double? Duration { get; set; }
        public int? Gain { get; set; }
        public bool? Save { get; set; }
        public double? ROI { get; set; }
        public BinningMode Binning { get; set; }
    }
}