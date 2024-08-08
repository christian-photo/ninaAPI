#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.Properties;
using ninaAPI.WebService.V2.GET;
using ninaAPI.WebService.V2.SET;
using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class ControllerV2 : WebApiController
    {
        private const string MISSING_API_KEY = "API Key is missing in the header";
        private const string INVALID_API_KEY = "API Key is not valid";
        private const string PROPERTY_NOT_SEND = "Property was not send";
        private const string INVALID_PROPERTY = "Property is not valid";
        private const string UNKNOWN_ERROR = "Unknown error";

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
        public void GetHistoryCount([QueryField] bool all = false, [QueryField] int index = 0, [QueryField] bool count = false)
        {

            if (!CheckSecurity())
            {
                return;
            }

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

        [Route(HttpVerbs.Get, "/socket-history")]
        public void GetSocketHistoryCount()
        {
            if (!CheckSecurity())
            {
                return;
            }

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

        [Route(HttpVerbs.Get, "/profile")]
        public void GetProfile([QueryField] bool active = true)
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                HttpContext.WriteToResponse(EquipmentMediatorV2.GetProfile(active));
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
                return;
            }
        }

        [Route(HttpVerbs.Get, "/sequence")]
        public void GetSequence()
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                HttpContext.WriteToResponse(EquipmentMediatorV2.GetSequence());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/image/{index}")]
        public async Task GetImage(int index, [QueryField] bool resize = false, [QueryField] int quality = -1, [QueryField] string size = "640x480")
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

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

        #endregion

        #region Equipment

        [Route(HttpVerbs.Get, "/equipment/{device}")]
        public async Task EquipmentHandler(string device, [QueryField] string action = "")
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (device)
                {
                    case "camera":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Camera));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Camera(action));
                        }
                        return;

                    case "telescope":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Telescope));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Telescope(action));
                        }
                        return;

                    case "focuser":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Focuser));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Focuser(action));
                        }
                        return;

                    case "filterwheel":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.FilterWheel));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.FilterWheel(action));
                        }
                        return;

                    case "guider":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Guider));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Guider(action));
                        }
                        return;

                    case "dome":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Dome));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Dome(action));
                        }
                        return;

                    case "rotator":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Rotator));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Rotator(action));
                        }
                        return;

                    case "safetymonitor":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.SafetyMonitor));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.SafetyMonitor(action));
                        }
                        return;

                    case "flatdevice":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.FlatDevice));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.FlatDevice(action));
                        }
                        return;

                    case "switch":
                        if (!string.IsNullOrEmpty(action))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Switch));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Switch(action));
                        }
                        return;

                    case "weather":
                        HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Weather)); // weather can't be controlled unfortunately :(
                        return;

                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(new Error("Unknown Device", 400)));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Post, "/profile")]
        public void SetProfile([QueryField] string action = "", [QueryField] string profileid = "", [QueryField] string settingpath = "", [QueryField] object newValue = null)
        {
            if (!CheckSecurity())
            {
                return;
            }

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

        [Route(HttpVerbs.Post, "/application")]
        public void Application([QueryField] string action = "", [QueryField] bool resize = false, [QueryField] int quality = -1, [QueryField] string size = "640x480", [QueryField] string tab = "")
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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

        #endregion

        public bool CheckKey(string key)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Utility.VerifyHash(sha, key, Settings.Default.ApiKey);
            }
        }

        public bool CheckSecurity()
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    Utility.CreateErrorTable(CommonErrors.INVALID_API_KEY);
                    return false;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                Utility.CreateErrorTable(CommonErrors.MISSING_API_KEY);
                return false;
            }
            return true;
        }
    }
}