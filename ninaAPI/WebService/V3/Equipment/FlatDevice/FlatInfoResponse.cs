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
using NINA.Equipment.Interfaces.Mediator;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public class FlatInfoResponse
    {
        public CoverState CoverState { get; set; }
        public bool LightOn { get; set; }
        public int Brightness { get; set; }
        public bool SupportsOpenClose { get; set; }
        public int MinBrightness { get; set; }
        public int MaxBrightness { get; set; }
        public bool SupportsOnOff { get; set; }
        public IList<string> SupportedActions { get; set; }
        public bool Connected { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DriverInfo { get; set; }
        public string DriverVersion { get; set; }
        public string DeviceId { get; set; }

        public FlatInfoResponse(FlatDeviceInfo info)
        {
            CoverState = info.CoverState;
            LightOn = info.LightOn;
            Brightness = info.Brightness;
            SupportsOpenClose = info.SupportsOpenClose;
            MinBrightness = info.MinBrightness;
            MaxBrightness = info.MaxBrightness;
            SupportsOnOff = info.SupportsOnOff;
            SupportedActions = info.SupportedActions;
            Connected = info.Connected;
            Name = info.Name;
            DisplayName = info.DisplayName;
            DriverInfo = info.DriverInfo;
            DriverVersion = info.DriverVersion;
            DeviceId = info.DeviceId;
        }
    }
}