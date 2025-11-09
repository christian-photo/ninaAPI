#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Collections.Generic;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public class FlatInfoResponse(FlatDeviceInfo info)
    {
        public CoverState CoverState { get; set; } = info.CoverState;
        public bool LightOn { get; set; } = info.LightOn;
        public int Brightness { get; set; } = info.Brightness;
        public bool SupportsOpenClose { get; set; } = info.SupportsOpenClose;
        public int MinBrightness { get; set; } = info.MinBrightness;
        public int MaxBrightness { get; set; } = info.MaxBrightness;
        public bool SupportsOnOff { get; set; } = info.SupportsOnOff;
        public IList<string> SupportedActions { get; set; } = info.SupportedActions;
        public bool Connected { get; set; } = info.Connected;
        public string Name { get; set; } = info.Name;
        public string DisplayName { get; set; } = info.DisplayName;
        public string DriverInfo { get; set; } = info.DriverInfo;
        public string DriverVersion { get; set; } = info.DriverVersion;
        public string DeviceId { get; set; } = info.DeviceId;
    }
}