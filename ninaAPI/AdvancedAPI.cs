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
using NINA.Astrometry.Interfaces;
using NINA.Core.Utility.WindowService;
using NINA.Profile;
using System;
using Settings = ninaAPI.Properties.Settings;
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3;

namespace ninaAPI
{
    [Export(typeof(IPluginManifest))]
    public class AdvancedAPI : PluginBase, INotifyPropertyChanged
    {
        public static NINAControls Controls;
        public static WebApiServer Server;

        public static string PluginId { get; private set; }
        private static AdvancedAPI instance;

        private Communicator communicator;


        public event PropertyChangedEventHandler PropertyChanged;
        private static IPluginOptionsAccessor PluginSettings;

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
                           IDomeFollower domeFollower,
                           ITwilightCalculator twilightCalculator,
                           IWindowServiceFactory windowFactory)
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/ninaAPI;component/WebService/V2/CustomDrivers/RotatorDataTemplate.xaml") });
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
                TwilightCalculator = twilightCalculator,
                WindowFactory = windowFactory,
            };

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            PluginSettings = new PluginOptionsAccessor(Controls.Profile, Guid.Parse(this.Identifier));
            Controls.Profile.ProfileChanged += ProfileChanged;

            UpdateDefaultPortCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                PreferredPort = ActualPort;
                ActualPort = PreferredPort; // This may look useless, but that way the visibility only changes when cachedPort changes and not when the user enters a new port
            });

            if (APIEnabled)
            {
                RunApi();
                ShowNotificationIfPortChanged();
            }

            communicator = new Communicator();

            SetHostNames();
            WebApiServer.StartWatchers();
        }

        private void RunApi()
        {
            ActualPort = NetworkUtility.GetNearestAvailablePort(PreferredPort);
            Server = new WebApiServer(ActualPort);
            if (SelectedApiOption == "V3")
            {
                Server.Start(new V3Api());
            }
            else if (SelectedApiOption == "V2")
            {
                Server.Start(new V2Api());
            }
            else if (SelectedApiOption == "Both")
            {
                Server.Start(new V2Api(), new V3Api());
            }
        }

        private void ProfileChanged(object sender, EventArgs e)
        {
            // Raise the event that this profile specific value has been changed due to the profile switch
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreferredPort)));
        }

        public static int GetActualPort()
        {
            return instance.ActualPort;
        }

        private void ShowNotificationIfPortChanged()
        {
            if (ActualPort != PreferredPort)
            {
                Notification.ShowInformation("Advanced API launched on a different port: " + ActualPort);
            }
        }

        public override Task Teardown()
        {
            Server?.Stop();
            Server = null;

            WebApiServer.StopWatchers();
            communicator.Dispose();

            FileSystemHelper.Cleanup();
            return base.Teardown();
        }

        public CommunityToolkit.Mvvm.Input.RelayCommand UpdateDefaultPortCommand { get; set; }

        private int actualPort = -1;
        public int ActualPort
        {
            get => actualPort;
            set
            {
                actualPort = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualPort)));
                PortVisibility = ((ActualPort != PreferredPort) && APIEnabled) ? Visibility.Visible : Visibility.Hidden;
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

        public bool ProfileDependentPort
        {
            get => Settings.Default.ProfileDependentPort;
            set
            {
                Settings.Default.ProfileDependentPort = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfileDependentPort)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreferredPort)));
            }
        }

        public int PreferredPort
        {
            get => ProfileDependentPort ? PluginSettings.GetValueInt32("Port", Settings.Default.Port) : Settings.Default.Port;
            set
            {
                if (ProfileDependentPort)
                {
                    PluginSettings.SetValueInt32("Port", value);
                }
                else
                {
                    Settings.Default.Port = value;
                    CoreUtil.SaveSettings(Settings.Default);
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreferredPort)));
            }
        }

        public bool CreateThumbnails
        {
            get => Settings.Default.CreateThumbnails;
            set
            {
                Settings.Default.CreateThumbnails = value;
                CoreUtil.SaveSettings(Settings.Default);
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
                    RunApi();
                    Notification.ShowSuccess("API successfully started");
                    ShowNotificationIfPortChanged();
                }
                else
                {
                    Server.Stop();
                    Server = null;
                    ActualPort = -1;
                    Notification.ShowSuccess("API successfully stopped");
                }
            }
        }

        public List<string> ApiOptions { get; } = ["Both", "V2", "V3"];
        public string SelectedApiOption
        {
            get => Settings.Default.SelectedApiOption;
            set
            {
                Settings.Default.SelectedApiOption = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedApiOption)));
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

        public string LocalAddress { get; set; }
        public string LocalNetworkAddress { get; set; }
        public string HostAddress { get; set; }

        private void SetHostNames()
        {
            string api = SelectedApiOption == "Both" || SelectedApiOption == "V3" ? "/v3/api" : "/v2/api";
            LocalAddress = $"http://{LocalAddresses.LocalHostName}:{ActualPort}{api}";
            LocalNetworkAddress = $"http://{LocalAddresses.IPAddress}:{ActualPort}{api}";
            HostAddress = $"http://{LocalAddresses.HostName}:{ActualPort}{api}";

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalAddress)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalNetworkAddress)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HostAddress)));
        }
    }
}
