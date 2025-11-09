#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;

namespace ninaAPI.WebService.V3.Equipment.Guider
{
    public class GuiderInfoResponse : GuiderInfo
    {
        public GuiderInfoResponse(IGuiderMediator guider)
        {
            var info = guider.GetInfo();
            CanClearCalibration = info.CanClearCalibration;
            CanSetShiftRate = info.CanSetShiftRate;
            CanGetLockPosition = info.CanGetLockPosition;
            SupportedActions = info.SupportedActions;
            RMSError = info.RMSError;
            PixelScale = info.PixelScale;
            Connected = info.Connected;
            Name = info.Name;
            DisplayName = info.DisplayName;
            Description = info.Description;
            DriverInfo = info.DriverInfo;
            DriverVersion = info.DriverVersion;
            DeviceId = info.DeviceId;

            IGuider device = (IGuider)guider.GetDevice();
            State = device?.State;
        }

        public GuiderInfoResponse() { }

        public string State { get; set; }
    }
}