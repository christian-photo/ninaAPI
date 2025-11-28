#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using NINA.Astrometry.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.WebService;
using Settings = ninaAPI.Properties.Settings;

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
#if WINDOWS
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/ninaAPI;component/WebService/V2/CustomDrivers/RotatorDataTemplate.xaml") });
#endif
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

        private void ProfileChanged(object sender, EventArgs e)
        {
            // Raise the event that this profile specific value has been changed due to the profile switch
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port)));
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
            Server?.Stop();
            Server = null;

            API.StopWatchers();
            communicator.Dispose();
            if (Directory.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"thumbnails-{Environment.ProcessId}")))
            {
                Retry.Do(() => Directory.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"thumbnails-{Environment.ProcessId}"), true), TimeSpan.FromMilliseconds(50), 3);
            }
            if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp.png")))
            {
                Retry.Do(() => File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp.png")), TimeSpan.FromMilliseconds(50), 3);
            }
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
                PortVisibility = ((CachedPort != Port) && APIEnabled) ? Visibility.Visible : Visibility.Hidden;
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port)));
            }
        }

        public int Port
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port)));
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
                    CachedPort = -1;
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

            LocalAdress = $"http://{dict["LOCALHOST"]}:{CachedPort}/v2/api";
            LocalNetworkAdress = $"http://{dict["IPADRESS"]}:{CachedPort}/v2/api";
            HostAdress = $"http://{dict["HOSTNAME"]}:{CachedPort}/v2/api";
        }
    }
}
