#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.WebService.V2.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class FWInfo
    {
        public FWInfo(FilterWheelInfo inf, FInfo[] filters)
        {
            Connected = inf.Connected;
            Name = inf.Name;
            DisplayName = inf.DisplayName;
            Description = inf.Description;
            DriverInfo = inf.DriverInfo;
            DriverVersion = inf.DriverVersion;
            DeviceId = inf.DeviceId;
            SupportedActions = inf.SupportedActions;
            IsMoving = inf.IsMoving;
            if (inf.Connected && inf.SelectedFilter != null)
                SelectedFilter = new FInfo { Name = inf.SelectedFilter?.Name, Id = inf.SelectedFilter.Position };
            AvailableFilters = filters;
        }

        public bool Connected { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string DriverInfo { get; set; }
        public string DriverVersion { get; set; }
        public string DeviceId { get; set; }
        public IList<string> SupportedActions { get; set; }
        public bool IsMoving { get; set; }
        public FInfo SelectedFilter { get; set; }


        public FInfo[] AvailableFilters { get; set; }
    }

    [Serializable]
    public class FInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }

        public static FInfo FromFilter(FilterInfo f)
        {
            return new FInfo() { Name = f.Name, Id = f.Position };
        }
    }

    public class FilterWheelWatcher : INinaWatcher, IFilterWheelConsumer
    {
        private readonly Func<object, EventArgs, Task> FilterWheelConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-CONNECTED");
        private readonly Func<object, EventArgs, Task> FilterWheelDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-DISCONNECTED");
        private readonly Func<object, FilterChangedEventArgs, Task> FilterWheelFilterChangedHandler = async (_, e) => await WebSocketV2.SendAndAddEvent(
            "FILTERWHEEL-CHANGED",
            new Dictionary<string, object>() { { "Previous", FInfo.FromFilter(e.From) }, { "New", FInfo.FromFilter(e.To) } });

        public void Dispose()
        {
            AdvancedAPI.Controls.FilterWheel.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.FilterWheel.Connected += FilterWheelConnectedHandler;
            AdvancedAPI.Controls.FilterWheel.Disconnected += FilterWheelDisconnectedHandler;
            AdvancedAPI.Controls.FilterWheel.FilterChanged += FilterWheelFilterChangedHandler;
            AdvancedAPI.Controls.FilterWheel.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.FilterWheel.Connected -= FilterWheelConnectedHandler;
            AdvancedAPI.Controls.FilterWheel.Disconnected -= FilterWheelDisconnectedHandler;
            AdvancedAPI.Controls.FilterWheel.FilterChanged -= FilterWheelFilterChangedHandler;
            AdvancedAPI.Controls.FilterWheel.RemoveConsumer(this);
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo)
        {
            WebSocketV2.SendConsumerEvent("FILTERWHEEL");
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/equipment/filterwheel/info")]
        public void FilterWheelInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                FInfo[] filters = profile.FilterWheelSettings.FilterWheelFilters.Select(f => new FInfo { Name = f.Name, Id = f.Position }).ToArray();

                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                FWInfo info = new FWInfo(filterwheel.GetInfo(), filters);
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/connect")]
        public async Task FilterWheelConnect([QueryField] bool skipRescan)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (!filterwheel.GetInfo().Connected)
                {
                    if (!skipRescan)
                    {
                        await filterwheel.Rescan();
                    }
                    await filterwheel.Connect();

                    response.Response = "Filterwheel connected";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/disconnect")]
        public async Task FilterWheelDisconnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                }
                response.Response = "Filterwheel disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/change-filter")]
        public void FilterWheelChangeFilter([QueryField] int filterId)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (!filterwheel.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Filterwheel not connected", 409));
                }
                else
                {
                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Filter not available", 409));
                    }
                    else
                    {
                        filterwheel.ChangeFilter(profile.FilterWheelSettings.FilterWheelFilters[filterId], progress: AdvancedAPI.Controls.StatusMediator.GetStatus());
                        response.Response = "Filter changed";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/filter-info")]
        public void FilterWheelFilterInfo([QueryField] int filterId)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (!filterwheel.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Filterwheel not connected", 409));
                }
                else
                {
                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Filter not available", 409));
                    }
                    else
                    {
                        response.Response = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                    }
                }
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
