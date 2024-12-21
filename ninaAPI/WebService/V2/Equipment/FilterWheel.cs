#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging.Filters;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using System;
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
            AvailableFilters = filters;
        }

        public bool Connected { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string DriverInfo { get; set; }
        public string DriverVersion { get; set; }
        public string DeviceId { get; set; }
        public bool CanSetFilter { get; set; }

        public FInfo[] AvailableFilters { get; set; }
    }

    public class FInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public partial class ControllerV2
    {
        public static void StartFilterWheelWatchers()
        {
            AdvancedAPI.Controls.FilterWheel.Connected += async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-CONNECTED");
            AdvancedAPI.Controls.FilterWheel.Disconnected += async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-DISCONNECTED");
            AdvancedAPI.Controls.FilterWheel.FilterChanged += async (_, _) => await WebSocketV2.SendAndAddEvent("FILTERWHEEL-CHANGED");
        }


        [Route(HttpVerbs.Get, "/equipment/filterwheel/info")]
        public void FilterWheelInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
        public async Task FilterWheelConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    await filterwheel.Connect();
                }
                response.Response = "Filterwheel connected";
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
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
