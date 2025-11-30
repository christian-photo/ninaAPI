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
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Equipment.Dome
{
    public class DomeInfoResponse : DomeInfo
    {
        public DomeInfoResponse(IDomeMediator dome, IDomeFollower follower)
        {
            CoreUtility.CopyProperties(dome.GetInfo(), this);
            IsFollowing = follower.IsFollowing;
            IsSynchronized = follower.IsSynchronized;
        }

        public bool IsFollowing { get; set; }
        public bool IsSynchronized { get; set; }
    }
}
