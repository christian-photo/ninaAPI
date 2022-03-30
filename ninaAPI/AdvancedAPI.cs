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
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Properties;
using ninaAPI.WebService;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using EmbedIO;
using System.Security.Cryptography;
using System;
using System.Text;

namespace ninaAPI
{
    [Export(typeof(IPluginManifest))]
    public class AdvancedAPI : PluginBase, INotifyPropertyChanged
    {
        public static NINAControls Controls;
        public API Server;


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
                           IApplicationMediator application)
        {
            if (string.IsNullOrEmpty(Settings.Default.ApiKey))
            {
                ApiKey = GenerateRandomKey(15);
            }
            
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
                Imaging = imaging,
                ImageHistory = history,
                Profile = profile,
                Sequence = sequence,
                StatusMediator = statusMediator,
                Application = application
            };

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }
            if (APIEnabled)
            {
                Server = new API();

                SetHostNames();
            }
            
            RestartAPI = new RelayCommand(o =>
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
                }
            });

            GenerateApiKeyCommand = new RelayCommand(o =>
            {
                ApiKey = GenerateRandomKey(12);
            });

            SetApiKeyCommand = new RelayCommand(o =>
            {
                SetApiKey(ApiKey);
            });

            if (Secure)
            {
                SecureVisibility = Visibility.Visible;
            }
            else
            {
                SecureVisibility = Visibility.Collapsed;
            }
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

        private string _apiKey;
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                _apiKey = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ApiKey)));
            }
        }

        private Visibility _secureVisibility;
        public Visibility SecureVisibility
        {
            get => _secureVisibility;
            set
            {
                _secureVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecureVisibility)));
            }
        }

        public bool Secure
        {
            get => Settings.Default.Secure;
            set
            {
                Settings.Default.Secure = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Secure)));
                if (Secure)
                {
                    SecureVisibility = Visibility.Visible;
                }
                else
                {
                    SecureVisibility = Visibility.Collapsed;
                }
            }
        }

        public string CertificatePath 
        {
            get => Settings.Default.CertificatePath;
            set
            {
                Settings.Default.CertificatePath = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CertificatePath)));
            }
        }

        public string CertificatePassword
        {
            get => Settings.Default.CertificatePassword;
            set
            {
                Settings.Default.CertificatePassword = value;
                CoreUtil.SaveSettings(Settings.Default);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CertificatePassword)));
            }
        }

        public RelayCommand RestartAPI { get; set; }
        public RelayCommand GenerateApiKeyCommand { get; set; }
        public RelayCommand SetApiKeyCommand { get; set; }

        public string GenerateRandomKey(int length)
        {
            using (RNGCryptoServiceProvider cryptRNG = new RNGCryptoServiceProvider())
            {
                byte[] tokenBuffer = new byte[length];
                cryptRNG.GetBytes(tokenBuffer);
                return Convert.ToBase64String(tokenBuffer);
            }
        }
        
        private void SetHostNames()
        {
            Dictionary<string, string> dict = Utility.GetLocalNames();
            if (Secure)
            {
                LocalAdress = $"https://{dict["LOCALHOST"]}:{Port}/api";
                LocalNetworkAdress = $"https://{dict["IPADRESS"]}:{Port}/api";
                HostAdress = $"https://{dict["HOSTNAME"]}:{Port}/api";
                return;
            }

            
            LocalAdress = $"http://{dict["LOCALHOST"]}:{Port}/api";
            LocalNetworkAdress = $"http://{dict["IPADRESS"]}:{Port}/api";
            HostAdress = $"http://{dict["HOSTNAME"]}:{Port}/api";
        }

        public void SetApiKey(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                Settings.Default.ApiKey = Utility.GetHash(sha256, key);
            }
        }
    }
}
