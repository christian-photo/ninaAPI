#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Interfaces;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace ninaAPI
{
    public class NINAControls
    {
        #region Equipment
        public ICameraMediator Camera;
        public ITelescopeMediator Mount;
        public IFocuserMediator Focuser;
        public IFilterWheelMediator FilterWheel;
        public IGuiderMediator Guider;
        public IRotatorMediator Rotator;
        public IFlatDeviceMediator FlatDevice;
        public IDomeMediator Dome;
        public ISwitchMediator Switch;
        public ISafetyMonitorMediator SafetyMonitor;
        public IAutoFocusVMFactory AutoFocusFactory;
        public IWeatherDataMediator Weather;
        public IDomeFollower DomeFollower;
        #endregion

        #region Image
        public IImagingMediator Imaging;
        public IImageHistoryVM ImageHistory;
        public IImageDataFactory ImageDataFactory;
        public IImageSaveMediator ImageSaveMediator;
        public IPlateSolverFactory PlateSolver;
        #endregion

        #region Application
        public IProfileService Profile;
        public ISequenceMediator Sequence;
        public IApplicationStatusMediator StatusMediator;
        public IApplicationMediator Application;
        public IMessageBroker MessageBroker;
        public IFramingAssistantVM FramingAssistant;
        public ITwilightCalculator TwilightCalculator;
        public INighttimeCalculator NighttimeCalculator;
        public IWindowServiceFactory WindowFactory;
        #endregion
    }
}
