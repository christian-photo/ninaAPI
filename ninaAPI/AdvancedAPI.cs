#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Properties;
using ninaAPI.WebService;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces;
using CommunityToolkit.Mvvm.Input;
using NINA.PlateSolving.Interfaces;

namespace ninaAPI
{
    [Export(typeof(IPluginManifest))]
    public class AdvancedAPI : PluginBase, INotifyPropertyChanged
    {
        public static NINAControls Controls;
        public static API Server;


        public event PropertyChangedEventHandler PropertyChanged;

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
                           IPlateSolverFactory platesolver)
        {
            
            Controls = new NINAControls()
            {
                Camera = camera,
                Mount = telescope,
                Focuser = focuser,
                FilterWheel = filterWheel,
                Guider = guider,
                Rotator = rotator,
                FlatDevice = flatDevice,
                Dome = dome,
                Switch = switches,
                SafetyMonitor = safety,
                Imaging = imaging,
                ImageHistory = history,
                Profile = profile,
                Sequence = sequence,
                StatusMediator = statusMediator,
                Application = application,
                ImageDataFactory = imageDataFactory,
                AutoFocusFactory = AFFactory,
                ImageSaveMediator = saveMediator,
                Weather = weather,
                PlateSolver = platesolver
            };

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
            }
            if (APIEnabled)
            {
                Server = new API();
                Server.Start();

                SetHostNames();
            }
            
            RestartAPI = new RelayCommand(() =>
            {
                if (Server != null)
                {
                    Server.Stop();
                    Server = null;
                }
                if (APIEnabled)
                {
                    SetHostNames();
                    Server = new API();
                    Server.Start();
                }
            });
        }

        public override Task Teardown()
        {
            if (Server != null)
            {
                Server.Stop();
                Server = null;
            }
            return base.Teardown();
        }

        public int Port
        {
            get => Settings.Default.Port;
            set
            {
                Settings.Default.Port = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public bool APIEnabled
        {
            get => Settings.Default.APIEnabled;
            set
            {
                Settings.Default.APIEnabled = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public bool UseV1
        {
            get => Settings.Default.StartV1;
            set
            {
                Settings.Default.StartV1 = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public bool UseV2
        {
            get => Settings.Default.StartV2;
            set
            {
                Settings.Default.StartV2 = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string LocalAdress
        {
            get => Settings.Default.LocalAdress;
            set
            {
                Settings.Default.LocalAdress = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalAdress)));
            }
        }

        public string LocalNetworkAdress
        {
            get => Settings.Default.LocalNetworkAdress;
            set
            {
                Settings.Default.LocalNetworkAdress = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalNetworkAdress)));
            }
        }

        public string HostAdress
        {
            get => Settings.Default.HostAdress;
            set
            {
                Settings.Default.HostAdress = value;
                NINA.Core.Utility.CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HostAdress)));
            }
        }

        public RelayCommand RestartAPI { get; set; }
        
        private void SetHostNames()
        {
            Dictionary<string, string> dict = Utility.GetLocalNames();
            
            LocalAdress = $"http://{dict["LOCALHOST"]}:{Port}/api";
            LocalNetworkAdress = $"http://{dict["IPADRESS"]}:{Port}/api";
            HostAdress = $"http://{dict["HOSTNAME"]}:{Port}/api";
        }
    }
}
