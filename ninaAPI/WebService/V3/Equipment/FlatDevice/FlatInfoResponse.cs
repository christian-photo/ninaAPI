#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Equipment.Equipment.MyFlatDevice;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public class FlatInfoResponse : FlatDeviceInfo
    {
        public FlatInfoResponse(FlatDeviceInfo info)
        {
            CoreUtility.CopyProperties(info, this);
        }
    }
}