#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraInfoResponse : CameraInfo
    {
        public CameraInfoResponse(ICameraMediator cam)
        {
            var info = cam.GetInfo();
            CoreUtility.CopyProperties(info, this);
        }

        public CameraInfoResponse() { }
    }
}