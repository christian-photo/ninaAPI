#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace ninaAPI
{
    public class NINAControls
    {
        #region Equipment
        public ICameraMediator Camera;
        public ITelescopeMediator Telescope;
        public IFocuserMediator Focuser;
        public IFilterWheelMediator FilterWheel;
        public IGuiderMediator Guider;
        public IRotatorMediator Rotator;
        public IFlatDeviceMediator FlatDevice;
        public IDomeMediator Dome;
        public ISwitchMediator Switch;
        public ISafetyMonitorMediator SafetyMonitor;
        #endregion

        #region Image
        public IImagingMediator Imaging;
        public IImageHistoryVM ImageHistory;
        #endregion

        #region Application
        public IProfileService Profile;
        public ISequenceMediator Sequence;
        public IApplicationStatusMediator StatusMediator;
        public IApplicationMediator Application;
        #endregion
    }
}
