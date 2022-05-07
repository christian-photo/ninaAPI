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
using Newtonsoft.Json;
using NINA.Core.Utility;
using ninaAPI.Properties;
using ninaAPI.WebService.GET;
using ninaAPI.WebService.SET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ninaAPI.WebService
{
    public class Controller : WebApiController
    {
        private readonly Hashtable FAILED_TABLE = new Hashtable() { { "Success", false } };
        private const string MISSING_API_KEY = "API Key is missing in the header";
        private const string INVALID_API_KEY = "API Key is not valid";
        private const string PROPERTY_NOT_SEND = "Property was not send";
        private const string INVALID_PROPERTY = "Property is not valid";
        private const string UNKNOWN_ERROR = "Unknown error";

        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return "ninaAPI: https://github.com/rennmaus-coder/ninaAPI/wiki";
        }

        #region GET

        [Route(HttpVerbs.Get, "/get/history")]
        public void GetHistoryCount([QueryField] string property, [QueryField] string parameter)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_API_KEY));
                    return;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(MISSING_API_KEY));
                return;
            }
            else if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Info($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "count":
                        HttpContext.WriteToResponse(EquipmentMediator.GetImageCount());
                        return;
                    case "list":
                        HttpContext.WriteToResponse(EquipmentMediator.GetImageHistory(int.Parse(parameter)));
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Get, "/get/profile")]
        public void GetProfile([QueryField] string property)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_API_KEY));
                    return;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(MISSING_API_KEY));
                return;
            }
            else if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Info($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                HttpContext.WriteToResponse(EquipmentMediator.GetProfile(property));
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(ex.Message));
                return;
            }
        }

        [Route(HttpVerbs.Get, "/get/sequence")]
        public void GetSequence([QueryField] string property, [QueryField] string parameter)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_API_KEY));
                    return;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(MISSING_API_KEY));
                return;
            }
            else if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Info($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "list":
                        HttpContext.WriteToResponse(EquipmentMediator.GetSequence(), new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Get, "/get/equipment")]
        public void GetInformation([QueryField] string property, [QueryField] string parameter)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_API_KEY));
                    return;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(MISSING_API_KEY));
                return;
            }
            else if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Info($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "camera":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Camera, parameter));
                        return;

                    case "telescope":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Telescope, parameter));
                        return;

                    case "focuser":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Focuser, parameter));
                        return;

                    case "filterwheel":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.FilterWheel, parameter));
                        return;

                    case "guider":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Guider, parameter));
                        return;

                    case "dome":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Dome, parameter));
                        return;

                    case "rotator":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Rotator, parameter));
                        return;

                    case "safetymonitor":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.SafteyMonitor, parameter));
                        return;

                    case "flatdevice":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.FlatDevice, parameter));
                        return;

                    case "switch":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Switch, parameter));
                        return;

                    case "image":
                        HttpContext.WriteToResponse(EquipmentMediator.GetLatestImage());
                        return;

                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(ex.Message));
            }
        }

        #endregion

        #region SET

        [Route(HttpVerbs.Get, "/set/equipment")]
        public async Task SetEquipment([QueryField] string property, [QueryField] string parameter, [QueryField] string value)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_API_KEY));
                    return;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(MISSING_API_KEY));
                return;
            }
            else if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Info($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "camera":
                        HttpContext.WriteToResponse(await EquipmentController.Camera(parameter, value));
                        return;

                    case "telescope":
                        HttpContext.WriteToResponse(await EquipmentController.Telescope(parameter, value));
                        return;

                    case "focuser":
                        HttpContext.WriteToResponse(await EquipmentController.Focuser(parameter, value));
                        return;

                    case "rotator":
                        HttpContext.WriteToResponse(await EquipmentController.Rotator(parameter, value));
                        return;

                    case "filterwheel":
                        HttpContext.WriteToResponse(await EquipmentController.FilterWheel(parameter, value));
                        return;

                    case "dome":
                        HttpContext.WriteToResponse(await EquipmentController.Dome(parameter, value));
                        return;

                    case "switch":
                        HttpContext.WriteToResponse(await EquipmentController.Switch(parameter, value));
                        return;

                    case "guider":
                        HttpContext.WriteToResponse(await EquipmentController.Guider(parameter, value));
                        return;

                    case "flatdevice":
                        HttpContext.WriteToResponse(await EquipmentController.FlatDevice(parameter, value));
                        return;

                    case "safteymonitor":
                        HttpContext.WriteToResponse(await EquipmentController.SafteyMonitor(parameter, value));
                        return;

                    case "sequence":
                        HttpContext.WriteToResponse(await EquipmentController.Sequence(parameter, value));
                        return;

                    case "application":
                        HttpContext.WriteToResponse(await EquipmentController.Application(parameter, value));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Get, "/set/profile")]
        public void SetProfile([QueryField] string property, [QueryField] string parameter, [QueryField] string value)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey))
                {
                    Logger.Error(INVALID_API_KEY);
                    HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_API_KEY));
                    return;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(MISSING_API_KEY);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(MISSING_API_KEY));
                return;
            }
            else if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            try
            {

                switch (property)
                {
                    case "switch":
                        HttpContext.WriteToResponse(EquipmentController.SwitchProfile(parameter));
                        return;
                    case "change-value":
                        HttpContext.WriteToResponse(EquipmentController.ChangeProfileValue(parameter, value));
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(ex.Message));
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
    }
}