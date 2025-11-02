#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;

namespace ninaAPI.WebService.V3.Equipment.Dome
{
    public class DomeInfoResponse : DomeInfo
    {
        public DomeInfoResponse(DomeInfo info, IDomeFollower follower)
        {
            Altitude = info.Altitude;
            Azimuth = info.Azimuth;
            ApplicationFollowing = info.ApplicationFollowing;
            AtHome = info.AtHome;
            AtPark = info.AtPark;
            CanFindHome = info.CanFindHome;
            CanPark = info.CanPark;
            CanSetAzimuth = info.CanSetAzimuth;
            CanSetPark = info.CanSetPark;
            CanSetShutter = info.CanSetShutter;
            CanSyncAzimuth = info.CanSyncAzimuth;
            DriverCanFollow = info.DriverCanFollow;
            DriverFollowing = info.DriverFollowing;
            ShutterStatus = info.ShutterStatus;
            Slewing = info.Slewing;
            SupportedActions = info.SupportedActions;
            IsFollowing = follower.IsFollowing;
            IsSynchronized = follower.IsSynchronized;
            DriverInfo = info.DriverInfo;
            DriverVersion = info.DriverVersion;
            Name = info.Name;
            DisplayName = info.DisplayName;
            Connected = info.Connected;
            Description = info.Description;
            DeviceId = info.DeviceId;
        }
        public bool IsFollowing { get; set; }
        public bool IsSynchronized { get; set; }
    }
}