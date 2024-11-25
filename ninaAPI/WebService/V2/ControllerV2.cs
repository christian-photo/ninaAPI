#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Common.Interfaces;
using CsvHelper.Configuration.Attributes;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using ninaAPI.Properties;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class ControllerV2 : WebApiController
    {

        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/rennmaus-coder/ninaAPI/wiki/V2";
        }

        #region GET

        [Route(HttpVerbs.Get, "/version")]
        public void GetVersion()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpContext.WriteToResponse(new HttpResponse() { Response = "1.0.0.0" });
        }

        [Route(HttpVerbs.Get, "/image-history")]
        public void GetHistoryCount([QueryField] bool all, [QueryField] int index, [QueryField] bool count)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (all)
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetImageHistory(-1));
                }
                else if (count)
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetImageHistoryCount());
                }
                else
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetImageHistory(index));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/event-history")]
        public void GetSocketHistoryCount()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                HttpContext.WriteToResponse(EquipmentMediatorV2.GetSocketEventHistory());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/sequence/{action}")]
        public async Task GetSequence(string action, [QueryField] bool skipValidation, [QueryField] string sequencename)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("json"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetSequence());
                }
                else if (action.Equals("list-available"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetAvailableSequences());
                }
                else
                {
                    HttpContext.WriteToResponse(EquipmentControllerV2.Sequence(action, skipValidation, sequencename, await HttpContext.GetRequestBodyAsStringAsync()));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/image/{index}")]
        public async Task GetImage(int index, [QueryField] bool resize, [QueryField] int quality, [QueryField] string size)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

            quality = Math.Clamp(quality, -1, 100);
            if (quality == 0)
                quality = -1; // quality should be set to -1 for png if omitted

            if (resize && string.IsNullOrWhiteSpace(size)) // workaround as default parameters are not working
                size = "640x480";

            try
            {
                if (resize)
                {
                    string[] s = size.Split('x');
                    int width = int.Parse(s[0]);
                    int height = int.Parse(s[1]);
                    HttpContext.WriteToResponse(await EquipmentMediatorV2.GetImage(quality, index, new Size(width, height)));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentMediatorV2.GetImage(quality, index, Size.Empty));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/profile/{action}")]
        public void SetProfile(string action, [QueryField] string profileid, [QueryField] string settingpath, [QueryField] object newValue, [QueryField] bool active)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (action)
                {
                    case "switch":
                        HttpContext.WriteToResponse(EquipmentControllerV2.SwitchProfile(profileid));
                        return;
                    case "change-value":
                        HttpContext.WriteToResponse(EquipmentControllerV2.ChangeProfileValue(settingpath, newValue));
                        return;
                    case "show":
                        HttpContext.WriteToResponse(EquipmentMediatorV2.GetProfile(active));
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/application/{action}")]
        public void Application(string action, [QueryField] bool resize, [QueryField] int quality, [QueryField] string size, [QueryField] string tab)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

            quality = Math.Clamp(quality, -1, 100);
            if (quality == 0)
                quality = -1; // quality should be set to -1 for png if omitted

            if (resize && string.IsNullOrWhiteSpace(size))
                size = "640x480";

            try
            {

                switch (action)
                {
                    case "switch-tab":
                        HttpContext.WriteToResponse(EquipmentControllerV2.Application(tab));
                        return;
                    case "screenshot":
                        if (resize)
                        {
                            string[] s = size.Split('x');
                            int width = int.Parse(s[0]);
                            int height = int.Parse(s[1]);
                            HttpContext.WriteToResponse(EquipmentMediatorV2.Screenshot(quality, new Size(width, height)));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.Screenshot(quality, Size.Empty));
                        }
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/framing/{action}")]
        public async Task Framing(string action, [QueryField] double RAangle, [QueryField] double DECangle, [QueryField] string slewoption, [QueryField] double rotation)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

            try
            {

                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.FramingInfo());
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.FramingAssistant(action, slewoption, RAangle, DECangle, rotation));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        #endregion

        #region Equipment

        [Route(HttpVerbs.Get, "/equipment/camera/{action}")]
        public async Task Camera(string action, [QueryField] bool solve, [QueryField] float duration, [QueryField] bool getResult, [QueryField] bool resize, [QueryField] int quality, [QueryField] string size)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

            quality = Math.Clamp(quality, -1, 100);
            if (quality == 0)
                quality = -1; // quality should be set to -1 for png if omitted

            if (resize && string.IsNullOrWhiteSpace(size))
                size = "640x480";


            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Camera));
                }
                else
                {
                    CaptureParameter captureParameter = new CaptureParameter();
                    captureParameter.solve = solve;
                    captureParameter.duration = duration;
                    captureParameter.getResult = getResult;
                    captureParameter.resize = resize;
                    captureParameter.quality = quality;
                    if (resize)
                    {
                        string[] s = size.Split('x');
                        int width = int.Parse(s[0]);
                        int height = int.Parse(s[1]);
                        captureParameter.size =  new Size(width, height);
                    }
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Camera(action, captureParameter));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/mount/{action}")]
        public async Task Mount(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Mount));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Mount(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/{action}")]
        public async Task Focuser(string action, [QueryField] int position)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Focuser));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Focuser(action, position));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/filterwheel/{action}")]
        public async Task Filterwheel(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.FilterWheel));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.FilterWheel(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/guider/{action}")]
        public async Task Guider(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Guider));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Guider(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/dome/{action}")]
        public async Task Dome(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Dome));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Dome(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/focuser/{action}")]
        public async Task Focuser(string action, [QueryField] float position)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Rotator));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Rotator(action, position));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/safetymonitor/{action}")]
        public async Task Safetymonitor(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.SafetyMonitor));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.SafetyMonitor(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/flatdevice/{action}")]
        public async Task Flatdevice(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.FlatDevice));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.FlatDevice(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/switch/{action}")]
        public async Task Switch(string action, [QueryField] short index, [QueryField] double value)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Switch));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Switch(action, index, value));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/equipment/weather/{action}")]
        public async Task Weather(string action)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("info"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Weather));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentControllerV2.Weather(action));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        #endregion
    }
}