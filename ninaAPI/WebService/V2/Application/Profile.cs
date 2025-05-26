#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using ninaAPI.Utility;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Core.Enum;
using System.Reflection;
using System.Collections.Generic;
using NINA.Profile;
using System.Linq;
using System.Collections.Specialized;
using NINA.Core.Model;

namespace ninaAPI.WebService.V2
{
    public class CustomAppSettings
    {
        public string Culture { get; set; }
        public double DevicePollingInterval { get; set; }
        public int PageSize { get; set; }
        public LogLevelEnum LogLevel { get; set; }

        internal static CustomAppSettings FromAppSettings(IApplicationSettings applicationSettings)
        {
            return new CustomAppSettings()
            {
                Culture = applicationSettings.Culture,
                DevicePollingInterval = applicationSettings.DevicePollingInterval,
                PageSize = applicationSettings.PageSize,
                LogLevel = applicationSettings.LogLevel
            };
        }
    }
    public class CustomColorSchemeSettings
    {
        public string AltColorSchema { get; set; }
        public string ColorSchema { get; set; }

        internal static CustomColorSchemeSettings FromColorSchemeSettings(IColorSchemaSettings colorSchemaSettings)
        {
            return new CustomColorSchemeSettings()
            {
                AltColorSchema = colorSchemaSettings.AltColorSchema.Name,
                ColorSchema = colorSchemaSettings.ColorSchema.Name,
            };
        }

    }
    public class ProfileResponse
    {
        private ProfileResponse() { }

        public static ProfileResponse FromProfile(IProfile profile)
        {
            return new ProfileResponse()
            {
                Name = profile.Name,
                Description = profile.Description,
                Id = profile.Id,
                LastUsed = profile.LastUsed,
                ApplicationSettings = CustomAppSettings.FromAppSettings(profile.ApplicationSettings),
                AstrometrySettings = profile.AstrometrySettings,
                CameraSettings = profile.CameraSettings,
                ColorSchemaSettings = CustomColorSchemeSettings.FromColorSchemeSettings(profile.ColorSchemaSettings),
                DomeSettings = profile.DomeSettings,
                FilterWheelSettings = profile.FilterWheelSettings,
                FlatWizardSettings = profile.FlatWizardSettings,
                FocuserSettings = profile.FocuserSettings,
                FramingAssistantSettings = profile.FramingAssistantSettings,
                GuiderSettings = profile.GuiderSettings,
                ImageFileSettings = profile.ImageFileSettings,
                ImageSettings = profile.ImageSettings,
                MeridianFlipSettings = profile.MeridianFlipSettings,
                PlanetariumSettings = profile.PlanetariumSettings,
                PlateSolveSettings = profile.PlateSolveSettings,
                RotatorSettings = profile.RotatorSettings,
                FlatDeviceSettings = profile.FlatDeviceSettings,
                SequenceSettings = profile.SequenceSettings,
                SwitchSettings = profile.SwitchSettings,
                TelescopeSettings = profile.TelescopeSettings,
                WeatherDataSettings = profile.WeatherDataSettings,
                SnapShotControlSettings = profile.SnapShotControlSettings,
                SafetyMonitorSettings = profile.SafetyMonitorSettings,
                // PluginSettings = profile.PluginSettings,
                AlpacaSettings = profile.AlpacaSettings,
                ImageHistorySettings = profile.ImageHistorySettings
            };
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid Id { get; set; }
        public DateTime LastUsed { get; set; }

        public CustomAppSettings ApplicationSettings { get; set; }
        public IAstrometrySettings AstrometrySettings { get; set; }
        public ICameraSettings CameraSettings { get; set; }
        public CustomColorSchemeSettings ColorSchemaSettings { get; set; }
        public IDomeSettings DomeSettings { get; set; }
        public IFilterWheelSettings FilterWheelSettings { get; set; }
        public IFlatWizardSettings FlatWizardSettings { get; set; }
        public IFocuserSettings FocuserSettings { get; set; }
        public IFramingAssistantSettings FramingAssistantSettings { get; set; }
        public IGuiderSettings GuiderSettings { get; set; }
        public IImageFileSettings ImageFileSettings { get; set; }
        public IImageSettings ImageSettings { get; set; }
        public IMeridianFlipSettings MeridianFlipSettings { get; set; }
        public IPlanetariumSettings PlanetariumSettings { get; set; }
        public IPlateSolveSettings PlateSolveSettings { get; set; }
        public IRotatorSettings RotatorSettings { get; set; }
        public IFlatDeviceSettings FlatDeviceSettings { get; set; }
        public ISequenceSettings SequenceSettings { get; set; }
        public ISwitchSettings SwitchSettings { get; set; }
        public ITelescopeSettings TelescopeSettings { get; set; }
        public IWeatherDataSettings WeatherDataSettings { get; set; }
        public ISnapShotControlSettings SnapShotControlSettings { get; set; }
        public ISafetyMonitorSettings SafetyMonitorSettings { get; set; }
        // public IPluginSettings PluginSettings { get; set; }, only provides methods
        public IAlpacaSettings AlpacaSettings { get; set; }
        public IImageHistorySettings ImageHistorySettings { get; set; }
    }

    public class ProfileWatcher : INinaWatcher
    {
        private readonly EventHandler ProfileChanged = new EventHandler((_, _) => WebSocketV2.SendAndAddEvent("PROFILE-CHANGED"));
        private readonly NotifyCollectionChangedEventHandler Profiles_CollectionChanged = new((_, list) =>
        {
            switch (list.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    WebSocketV2.SendAndAddEvent("PROFILE-ADDED");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    WebSocketV2.SendAndAddEvent("PROFILE-REMOVED");
                    break;
            }
        });

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Profile.Profiles.CollectionChanged += Profiles_CollectionChanged;
            AdvancedAPI.Controls.Profile.ProfileChanged += ProfileChanged;
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Profile.Profiles.CollectionChanged -= Profiles_CollectionChanged;
            AdvancedAPI.Controls.Profile.ProfileChanged -= ProfileChanged;
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/profile/show")]
        public void ProfileShow([QueryField] bool active)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IProfileService profileService = AdvancedAPI.Controls.Profile;

                if (!active)
                {
                    response.Response = profileService.Profiles;
                }
                else
                {
                    response.Response = ProfileResponse.FromProfile(profileService.ActiveProfile);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/profile/change-value")]
        public void ProfileChangeValue([QueryField] string settingpath, [QueryField] object newValue)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                if (string.IsNullOrEmpty(settingpath))
                {
                    response = CoreUtility.CreateErrorTable(new Error("Invalid path", 400));
                }
                else if (newValue is null)
                {
                    response = CoreUtility.CreateErrorTable(new Error("New value can't be null", 400));
                }
                else
                {

                    string[] pathSplit = settingpath.Split('-'); // e.g. 'CameraSettings-PixelSize' -> CameraSettings, PixelSize
                    object position = AdvancedAPI.Controls.Profile.ActiveProfile;

                    if (pathSplit.Length == 1)
                    {
                        position.GetType().GetProperty(settingpath).SetValue(position, newValue);
                    }
                    else
                    {
                        for (int i = 0; i <= pathSplit.Length - 2; i++)
                        {
                            position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
                        }
                        PropertyInfo prop = position.GetType().GetProperty(pathSplit[^1]);
                        prop.SetValue(position, ((string)newValue).CastString(prop.PropertyType));
                    }

                    response.Response = "Updated setting";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/profile/switch")]
        public void ProfileSwitch([QueryField] string profileid)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                Guid guid = Guid.Parse(profileid);
                IEnumerable<ProfileMeta> x = AdvancedAPI.Controls.Profile.Profiles.Where(x => x.Id == guid);
                if (x.Any())
                {
                    ProfileMeta profile = x.First();
                    AdvancedAPI.Controls.Profile.SelectProfile(profile);
                    response.Response = "Successfully switched profile";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("No profile with specified id found!", 400));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        class HorizonResponse
        {
            public double[] Altitudes { get; set; }
            public double[] Azimuths { get; set; }

            public HorizonResponse(CustomHorizon horizon)
            {
                if (horizon == null)
                {
                    Altitudes = [];
                    Azimuths = [];
                    return;
                }
                Altitudes = horizon.GetType().GetField("altitudes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(horizon) as double[];
                Azimuths = horizon.GetType().GetField("azimuths", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(horizon) as double[];
            }
        }

        [Route(HttpVerbs.Get, "/profile/horizon")]
        public void ProfileHorizon()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                response.Response = new HorizonResponse(AdvancedAPI.Controls.Profile.ActiveProfile.AstrometrySettings.Horizon);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }


            HttpContext.WriteToResponse(response);
        }
    }
}
