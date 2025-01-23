#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
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
using NINA.PlateSolving.Interfaces;
using ninaAPI.Utility;
using NINA.Core.Utility.Notification;
using System.Windows;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;

namespace ninaAPI
{
    [Export(typeof(IPluginManifest))]
    public class AdvancedAPI : PluginBase, INotifyPropertyChanged
    {
        public static NINAControls Controls;
        public static API Server;

        public static string PluginId { get; private set; }
        private static AdvancedAPI instance;

        private Communicator communicator;


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
                           IPlateSolverFactory platesolver,
                           IMessageBroker broker,
                           IFramingAssistantVM framing,
                           IDomeFollower domeFollower)
        {

            PluginId = this.Identifier;
            instance = this;

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
                PlateSolver = platesolver,
                MessageBroker = broker,
                FramingAssistant = framing,
                DomeFollower = domeFollower,
            };

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            UpdateDefaultPortCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                Port = CachedPort;
                CachedPort = Port; // This may look useless, but that way the visibility only changes when cachedPort changes and not when the user enters a new port
            });

            if (APIEnabled)
            {
                CachedPort = CoreUtility.GetNearestAvailablePort(Port);
                Server = new API(CachedPort);
                Server.Start();
                ShowNotificationIfPortChanged();
            }

            communicator = new Communicator();

            SetHostNames();
            API.StartWatchers();
        }

        public static int GetCachedPort()
        {
            return instance.CachedPort;
        }

        private void ShowNotificationIfPortChanged()
        {
            if (CachedPort != Port)
            {
                Notification.ShowInformation("Advanced API launched on a different port: " + CachedPort);
            }
        }

        public override Task Teardown()
        {
            if (Server != null)
            {
                Server.Stop();
                Server = null;
            }
            API.StopWatchers();
            communicator.Dispose();
            return base.Teardown();
        }

        public CommunityToolkit.Mvvm.Input.RelayCommand UpdateDefaultPortCommand { get; set; }

        private int cachedPort = -1;
        public int CachedPort
        {
            get => cachedPort;
            set
            {
                cachedPort = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CachedPort)));
                PortVisibility = CachedPort == Port ? Visibility.Hidden : Visibility.Visible;
                SetHostNames();
            }
        }

        private Visibility portVisibility = Visibility.Hidden;
        public Visibility PortVisibility
        {
            get => portVisibility;
            set
            {
                portVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PortVisibility)));
            }
        }

        public int Port
        {
            get => Settings.Default.Port;
            set
            {
                Settings.Default.Port = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port)));
            }
        }

        public bool APIEnabled
        {
            get => Settings.Default.APIEnabled;
            set
            {
                Settings.Default.APIEnabled = value;
                CoreUtil.SaveSettings(Settings.Default);
                if (value)
                {
                    CachedPort = CoreUtility.GetNearestAvailablePort(Port);
                    Server = new API(CachedPort);
                    Server.Start();
                    Notification.ShowSuccess("API successfully started");
                    ShowNotificationIfPortChanged();

                }
                else
                {
                    Server.Stop();
                    Server = null;
                    Notification.ShowSuccess("API successfully stopped");
                }
            }
        }

        public bool UseV2
        {
            get => Settings.Default.StartV2;
            set
            {
                Settings.Default.StartV2 = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public bool UseAccessHeader
        {
            get => Settings.Default.UseAccessControlHeader;
            set
            {
                Settings.Default.UseAccessControlHeader = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string LocalAdress
        {
            get => Settings.Default.LocalAdress;
            set
            {
                Settings.Default.LocalAdress = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalAdress)));
            }
        }

        public string LocalNetworkAdress
        {
            get => Settings.Default.LocalNetworkAdress;
            set
            {
                Settings.Default.LocalNetworkAdress = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalNetworkAdress)));
            }
        }

        public string HostAdress
        {
            get => Settings.Default.HostAdress;
            set
            {
                Settings.Default.HostAdress = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HostAdress)));
            }
        }

        private void SetHostNames()
        {
            Dictionary<string, string> dict = CoreUtility.GetLocalNames();

            LocalAdress = $"http://{dict["LOCALHOST"]}:{CachedPort}/api";
            LocalNetworkAdress = $"http://{dict["IPADRESS"]}:{CachedPort}/api";
            HostAdress = $"http://{dict["HOSTNAME"]}:{CachedPort}/api";
        }
    }
}
