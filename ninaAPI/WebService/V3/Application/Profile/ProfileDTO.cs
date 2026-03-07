#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using NINA.Profile;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.WebService.V3.Equipment.FilterWheel;

namespace ninaAPI.WebService.V3.Application.Profile
{
    public class ProfileDTO : IProfile
    {
        public ProfileDTO(IProfile profile)
        {
            CoreUtility.CopyProperties(profile, this);
            ApplicationSettings = new ApplicationSettingsDTO(profile.ApplicationSettings as ApplicationSettings);
            FilterWheelSettings = new FilterWheelSettingsDTO(profile.FilterWheelSettings as FilterWheelSettings);
            PlateSolveSettings = new PlateSolveSettingsDTO(profile.PlateSolveSettings as PlateSolveSettings);
            SnapShotControlSettings = new SnapShotControlSettingsDTO(profile.SnapShotControlSettings as SnapShotControlSettings);
            FramingAssistantSettings = null; // Use the dedicated framing endpoints
            PluginSettings = null; // Cant change anyway
            DockPanelSettings = null; // Not something you should or need to change via the api
        }

        public Guid Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public DateTime LastUsed { get; set; }

        public IApplicationSettings ApplicationSettings { get; set; }
        public IAstrometrySettings AstrometrySettings { get; set; }
        public ICameraSettings CameraSettings { get; set; }
        public IColorSchemaSettings ColorSchemaSettings { get; set; }
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
        public IPluginSettings PluginSettings { get; set; }
        public IGnssSettings GnssSettings { get; set; }
        public IAlpacaSettings AlpacaSettings { get; set; }
        public IImageHistorySettings ImageHistorySettings { get; set; }
        public IDockPanelSettings DockPanelSettings { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
    public class ApplicationSettingsDTO : ApplicationSettings
    {
        public ApplicationSettingsDTO(ApplicationSettings applicationSettings)
        {
            CoreUtility.CopyProperties(applicationSettings, this);
            SkyAtlasImageRepository = null;
            SelectedPluggableBehaviors = null;
        }
    }

    public class SnapShotControlSettingsDTO : SnapShotControlSettings
    {
        public SnapShotControlSettingsDTO(SnapShotControlSettings snapShotControlSettings)
        {
            CoreUtility.CopyProperties(snapShotControlSettings, this);
            SnapShotFilter = FilterData.FromFilterShort(snapShotControlSettings.Filter);
            Filter = null;
        }

        public FilterData SnapShotFilter { get; set; }
    }

    public class PlateSolveSettingsDTO : PlateSolveSettings
    {
        public PlateSolveSettingsDTO(PlateSolveSettings plateSolveSettings)
        {
            CoreUtility.CopyProperties(plateSolveSettings, this);
            PlateSolveFilter = FilterData.FromFilterShort(plateSolveSettings.Filter);
            Filter = null;
        }

        public FilterData PlateSolveFilter { get; set; }
    }

    public class FilterWheelSettingsDTO : FilterWheelSettings
    {
        public FilterWheelSettingsDTO(FilterWheelSettings filterWheelSettings)
        {
            CoreUtility.CopyProperties(filterWheelSettings, this);
            FilterWheelFilters = null;
        }
    }
}