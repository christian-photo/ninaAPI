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
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3;
using System.Runtime.CompilerServices;
using ninaAPI.WebService.Interfaces;

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
                PreferredPort = ActualPort;
                ActualPort = PreferredPort; // This may look useless, but that way the visibility only changes when cachedPort changes and not when the user enters a new port
            });

            V3Api.StartEventWatchers(); // THis has to be done before the API is started because the event socket needs to be initialized

            if (APIEnabled)
            {
                RunApi();
                ShowNotificationIfPortChanged();
            }

            communicator = new Communicator();

            SetHostNames();
            WebApiServer.StartWatchers();
        }

        private IHttpApi V3 { get; set; }
        private IHttpApi V2 { get; set; }

        private void RunApi()
        {
            ActualPort = NetworkUtility.GetNearestAvailablePort(PreferredPort);
            Server = new WebApiServer(ActualPort);
            if (SelectedApiOption == "V3")
            {
                V3 ??= new V3Api();
                Server.Start(V3);
            }
            else if (SelectedApiOption == "V2")
            {
                V2 ??= new V2Api();
                Server.Start(V2);
            }
            else if (SelectedApiOption == "Both")
            {
                V2 ??= new V2Api();
                V3 ??= new V3Api();
                Server.Start(V2, V3);
            }
        }

        private void ProfileChanged(object sender, EventArgs e)
        {
            // Raise the event that this profile specific value has been changed due to the profile switch
            RaisePropertyChanged(nameof(PreferredPort));
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
            V3Api.StopEventWatchers();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }

        public bool ProfileDependentPort
        {
            get => Settings.Default.ProfileDependentPort;
            set
            {
                Settings.Default.ProfileDependentPort = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PreferredPort));
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
                RaisePropertyChanged();
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

        public bool APIEnabled => SelectedApiOption != "Off";

        public List<string> ApiOptions { get; } = ["Both", "V2", "V3", "Off"];
        public string SelectedApiOption
        {
            get => Settings.Default.SelectedApiOption;
            set
            {
                if (value == SelectedApiOption)
                    return;

                Settings.Default.SelectedApiOption = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();

                Server?.Stop();
                Server = null;

                if (value == "Off")
                {
                    ActualPort = -1;
                    Notification.ShowSuccess("API successfully stopped");
                }
                else
                {
                    RunApi();
                    Notification.ShowSuccess("API successfully started");
                    ShowNotificationIfPortChanged();
                }
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

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
