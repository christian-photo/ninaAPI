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
using NINA.Sequencer.Logic;
using Microsoft.Extensions.DependencyInjection;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using System.Security;
using ninaAPI.WebService.V3.Equipment.Camera;

namespace ninaAPI
{
    [Export(typeof(IPluginManifest))]
    public class AdvancedAPI : PluginBase, INotifyPropertyChanged
    {
        public static IMediatorContainer Controls { get; private set; }
        public static WebApiServer Server;

        public static string PluginId { get; private set; }
        private static AdvancedAPI instance;

        private Communicator communicator;
        private ServiceCollection services;

        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;


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
                           INighttimeCalculator nighttimeCalculator,
                           IWindowServiceFactory windowFactory,
                           IMeridianFlipVMFactory meridianFlipVMFactory,
                           ISymbolBroker symbolBroker)
        {
#if WINDOWS
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/ninaAPI;component/WebService/V2/CustomDrivers/RotatorDataTemplate.xaml") });
#endif
            PluginId = this.Identifier;
            instance = this;

            Controls = new NINAControls(
                camera,
                telescope,
                focuser,
                filterWheel,
                guider,
                rotator,
                flatDevice,
                dome,
                switches,
                safety,
                imaging,
                history,
                profile,
                sequence,
                statusMediator,
                application,
                imageDataFactory,
                AFFactory,
                saveMediator,
                weather,
                platesolver,
                broker,
                framing,
                domeFollower,
                twilightCalculator,
                nighttimeCalculator,
                windowFactory,
                meridianFlipVMFactory,
                symbolBroker
            );

            services = new ServiceCollection();
            services.AddSingleton(camera);
            services.AddSingleton(telescope);
            services.AddSingleton(focuser);
            services.AddSingleton(filterWheel);
            services.AddSingleton(guider);
            services.AddSingleton(rotator);
            services.AddSingleton(flatDevice);
            services.AddSingleton(dome);
            services.AddSingleton(switches);
            services.AddSingleton(safety);
            services.AddSingleton(imaging);
            services.AddSingleton(history);
            services.AddSingleton(profile);
            services.AddSingleton(sequence);
            services.AddSingleton(statusMediator);
            services.AddSingleton(application);
            services.AddSingleton(imageDataFactory);
            services.AddSingleton(AFFactory);
            services.AddSingleton(saveMediator);
            services.AddSingleton(weather);
            services.AddSingleton(platesolver);
            services.AddSingleton(broker);
            services.AddSingleton(framing);
            services.AddSingleton(domeFollower);
            services.AddSingleton(twilightCalculator);
            services.AddSingleton(nighttimeCalculator);
            services.AddSingleton(windowFactory);
            services.AddSingleton(meridianFlipVMFactory);
            services.AddSingleton(symbolBroker);

            processMediator = new ApiProcessMediator();
            serializer = SerializerFactory.GetSerializer();

            services.AddSingleton(processMediator);
            services.AddSingleton(serializer);
            services.AddSingleton(new CaptureMediator(camera, filterWheel, profile, imaging, saveMediator, statusMediator, processMediator));

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            SimpleW.Observability.Log.SetSink((entry) => Logger.Info(entry.Message, entry.Source));

            PluginSettings = new PluginOptionsAccessor(profile, Guid.Parse(this.Identifier));
            profile.ProfileChanged += ProfileChanged;

            UpdateDefaultPortCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                PreferredPort = ActualPort;
                ActualPort = PreferredPort; // This may look useless, but that way the visibility only changes when cachedPort changes and not when the user enters a new port
            });

            using (ServiceProvider provider = services.BuildServiceProvider())
            {
                V2Api.StartWatchers();
                V3Api.StartWatchers(provider); // This has to be done before the API is started because the event socket needs to be initialized
            }

            if (APIEnabled)
            {
                RunApi();
                ShowNotificationIfPortChanged();
            }

            communicator = new Communicator();

            SetHostNames();
        }

        public static IHttpApi V3 { get; private set; }
        public static IHttpApi V2 { get; private set; }

        private void RunApi()
        {
            ServiceProvider provider = services.BuildServiceProvider();
            ActualPort = NetworkUtility.GetNearestAvailablePort(PreferredPort);
            Server = new WebApiServer(ActualPort);
            if (SelectedApiOption == "V3")
            {
                V3 ??= new V3Api();
                Server.Start(provider, V3).ConfigureAwait(false);
            }
            else if (SelectedApiOption == "V2")
            {
                V2 ??= new V2Api();
                Server.Start(provider, V2).ConfigureAwait(false);
            }
            else if (SelectedApiOption == "Both")
            {
                V2 ??= new V2Api();
                V3 ??= new V3Api();
                Server.Start(provider, V2, V3).ConfigureAwait(false);
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

            V2Api.StopWatchers();
            V3Api.StopWatchers();
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

        public int ThumbnailLongAxis
        {
            get => Settings.Default.ThumbnailLongAxis;
            set
            {
                if (value > 1)
                {
                    Settings.Default.ThumbnailLongAxis = value;
                    CoreUtil.SaveSettings(Settings.Default);
                }
            }
        }

        public bool EnableTelemetry
        {
            get => Settings.Default.EnableTelemetry;
            set
            {
                Settings.Default.EnableTelemetry = value;
                CoreUtil.SaveSettings(Settings.Default);
                if (Server?.Server != null)
                {
                    if (value)
                    {
                        Server.Server.EnableTelemetry();
                    }
                    else
                    {
                        Server.Server.DisableTelemetry();
                    }
                }
            }
        }

        public bool UseSSL
        {
            get => Settings.Default.UseSSL;
            set
            {
                Settings.Default.UseSSL = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string SSLCertificatePath
        {
            get => Settings.Default.SSLCertificatePath;
            set
            {
                Settings.Default.SSLCertificatePath = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string SSLPassword
        {
            get => Settings.Default.SSLPassword;
            set
            {
                Settings.Default.SSLPassword = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public bool UseAuth
        {
            get => Settings.Default.UseAuth;
            set
            {
                Settings.Default.UseAuth = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string AuthUsername
        {
            get => Settings.Default.AuthUsername;
            set
            {
                Settings.Default.AuthUsername = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string AuthPassword
        {
            get => Settings.Default.AuthPassword;
            set
            {
                Settings.Default.AuthPassword = value;
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
            string protocol = UseSSL ? "https" : "http";
            LocalAddress = $"{protocol}://{LocalAddresses.LocalHostName}:{ActualPort}{api}";
            LocalNetworkAddress = $"{protocol}://{LocalAddresses.IPAddress}:{ActualPort}{api}";
            HostAddress = $"{protocol}://{LocalAddresses.HostName}:{ActualPort}{api}";

            RaisePropertyChanged(nameof(LocalAddress));
            RaisePropertyChanged(nameof(LocalNetworkAddress));
            RaisePropertyChanged(nameof(HostAddress));
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
