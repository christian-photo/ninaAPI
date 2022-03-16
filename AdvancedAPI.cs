#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using ninaAPI.Properties;
using ninaAPI.WebService;
using System.ComponentModel.Composition;
using EmbedIO;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Mediator;

namespace ninaAPI
{
    [Export(typeof(IPluginManifest))]
    public class AdvancedAPI : PluginBase
    {
        public static NINAControls Controls;
        public API Server;

        [ImportingConstructor]
        public AdvancedAPI(ICameraMediator camera,
                           ITelescopeMediator telescope,
                           IFocuserMediator focuser,
                           IFilterWheelMediator filterWheel,
                           IGuiderMediator guider,
                           IRotatorMediator rotator,
                           IFlatDeviceMediator flatDevice,
                           IDomeMediator dome,
                           ISwitchMediator switches,
                           ISafetyMonitorMediator safety,
                           IWeatherDataMediator weather,
                           IImagingMediator imaging,
                           IImageHistoryVM history,
                           IProfileService profile)
        {
            Controls = new NINAControls()
            {
                Camera = camera,
                Telescope = telescope,
                Focuser = focuser,
                FilterWheel = filterWheel,
                Guider = guider,
                Rotator = rotator,
                FlatDevice = flatDevice,
                Dome = dome,
                Switch = switches,
                SafetyMonitor = safety,
                Weather = weather,
                Imaging = imaging,
                ImageHistory = history,
                Profile = profile,
                Sequence = new SequenceMediator()
            };



            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }
            Server = new API();
        }

        public override Task Teardown()
        {
            Server.Stop();
            return base.Teardown();
        }
    }
}
