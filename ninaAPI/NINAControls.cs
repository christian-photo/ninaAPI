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
using NINA.Sequencer.Logic;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace ninaAPI
{
    public interface IMediatorContainer
    {
        #region Equipment
        public ICameraMediator Camera { get; }
        public ITelescopeMediator Mount { get; }
        public IFocuserMediator Focuser { get; }
        public IFilterWheelMediator FilterWheel { get; }
        public IGuiderMediator Guider { get; }
        public IRotatorMediator Rotator { get; }
        public IFlatDeviceMediator FlatDevice { get; }
        public IDomeMediator Dome { get; }
        public ISwitchMediator Switch { get; }
        public ISafetyMonitorMediator SafetyMonitor { get; }
        public IAutoFocusVMFactory AutoFocusFactory { get; }
        public IMeridianFlipVMFactory MeridianFlipFactory { get; }
        public IWeatherDataMediator Weather { get; }
        public IDomeFollower DomeFollower { get; }
        #endregion

        #region Image
        public IImagingMediator Imaging { get; }
        public IImageHistoryVM ImageHistory { get; }
        public IImageDataFactory ImageDataFactory { get; }
        public IImageSaveMediator ImageSaveMediator { get; }
        public IPlateSolverFactory PlateSolver { get; }
        #endregion

        #region Application
        public IProfileService Profile { get; }
        public ISequenceMediator Sequence { get; }
        public IApplicationStatusMediator StatusMediator { get; }
        public IApplicationMediator Application { get; }
        public IMessageBroker MessageBroker { get; }
        public IFramingAssistantVM FramingAssistant { get; }
        public ITwilightCalculator TwilightCalculator { get; }
        public INighttimeCalculator NighttimeCalculator { get; }
        public IWindowServiceFactory WindowFactory { get; }
        public ISymbolBroker SymbolBroker { get; }
        #endregion
    }

    public class NINAControls(
        ICameraMediator camera,
        ITelescopeMediator mount,
        IFocuserMediator focuser,
        IFilterWheelMediator filterWheel,
        IGuiderMediator guider,
        IRotatorMediator rotator,
        IFlatDeviceMediator flatDevice,
        IDomeMediator dome,
        ISwitchMediator switches,
        ISafetyMonitorMediator safety,
        IImagingMediator imaging,
        IImageHistoryVM history,
        IProfileService profile,
        ISequenceMediator sequence,
        IApplicationStatusMediator statusMediator,
        IApplicationMediator application,
        IImageDataFactory imageDataFactory,
        IAutoFocusVMFactory AFFactory,
        IImageSaveMediator saveMediator,
        IWeatherDataMediator weather,
        IPlateSolverFactory platesolver,
        IMessageBroker broker,
        IFramingAssistantVM framing,
        IDomeFollower domeFollower,
        ITwilightCalculator twilightCalculator,
        INighttimeCalculator nighttimeCalculator,
        IWindowServiceFactory windowFactory,
        IMeridianFlipVMFactory meridianFlipVMFactory,
        ISymbolBroker symbolBroker
    ) : IMediatorContainer
    {
        public ICameraMediator Camera { get; } = camera;

        public ITelescopeMediator Mount { get; } = mount;

        public IFocuserMediator Focuser { get; } = focuser;

        public IFilterWheelMediator FilterWheel { get; } = filterWheel;

        public IGuiderMediator Guider { get; } = guider;

        public IRotatorMediator Rotator { get; } = rotator;

        public IFlatDeviceMediator FlatDevice { get; } = flatDevice;

        public IDomeMediator Dome { get; } = dome;

        public ISwitchMediator Switch { get; } = switches;

        public ISafetyMonitorMediator SafetyMonitor { get; } = safety;

        public IAutoFocusVMFactory AutoFocusFactory { get; } = AFFactory;

        public IMeridianFlipVMFactory MeridianFlipFactory { get; } = meridianFlipVMFactory;

        public IWeatherDataMediator Weather { get; } = weather;

        public IDomeFollower DomeFollower { get; } = domeFollower;

        public IImagingMediator Imaging { get; } = imaging;

        public IImageHistoryVM ImageHistory { get; } = history;

        public IImageDataFactory ImageDataFactory { get; } = imageDataFactory;

        public IImageSaveMediator ImageSaveMediator { get; } = saveMediator;

        public IPlateSolverFactory PlateSolver { get; } = platesolver;

        public IProfileService Profile { get; } = profile;

        public ISequenceMediator Sequence { get; } = sequence;

        public IApplicationStatusMediator StatusMediator { get; } = statusMediator;

        public IApplicationMediator Application { get; } = application;

        public IMessageBroker MessageBroker { get; } = broker;

        public IFramingAssistantVM FramingAssistant { get; } = framing;

        public ITwilightCalculator TwilightCalculator { get; } = twilightCalculator;

        public INighttimeCalculator NighttimeCalculator { get; } = nighttimeCalculator;

        public IWindowServiceFactory WindowFactory { get; } = windowFactory;

        public ISymbolBroker SymbolBroker { get; } = symbolBroker;
    }
}
