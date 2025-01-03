﻿#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.Utility;
using ninaAPI.WebService.V1.GET;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V1
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
            return $"ninaAPI: https://github.com/rennmaus-coder/ninaAPI/wiki";
        }

        #region GET

        [Route(HttpVerbs.Get, "/version")]
        public void GetVersion()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpContext.WriteToResponse(new HttpResponse() { Response = "1.0.1.2" });
        }

        [Route(HttpVerbs.Get, "/history")]
        public void GetHistoryCount([QueryField] string property, [QueryField] string parameter)
        {
            if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
                    case "socket":
                        HttpContext.WriteToResponse(EquipmentMediator.GetSocketImageHistory(int.Parse(parameter)));
                        return;
                    default:
                        HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Get, "/socket-history")]
        public void GetSocketHistoryCount([QueryField] string property, [QueryField] string parameter)
        {
            if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "count":
                        HttpContext.WriteToResponse(EquipmentMediator.GetSocketImageCount());
                        return;
                    case "list":
                        HttpContext.WriteToResponse(EquipmentMediator.GetSocketImageHistory(int.Parse(parameter)));
                        return;
                    case "events":
                        HttpContext.WriteToResponse(EquipmentMediator.GetSocketEventHistory());
                        return;
                    default:
                        HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Get, "/profile")]
        public void GetProfile([QueryField] string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                HttpContext.WriteToResponse(EquipmentMediator.GetProfile(property));
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
                return;
            }
        }

        [Route(HttpVerbs.Get, "/sequence")]
        public void GetSequence([QueryField] string property, [QueryField] string parameter)
        {
            if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "list":
                        HttpContext.WriteToResponse(EquipmentMediator.GetSequence());
                        return;
                    default:
                        HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Get, "/equipment")]
        public async Task GetInformation([QueryField] string property, [QueryField] string parameter, [QueryField] string index)
        {
            if (string.IsNullOrEmpty(property))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (property)
                {
                    case "camera":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Camera));
                        return;

                    case "telescope":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Mount));
                        return;

                    case "focuser":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Focuser));
                        return;

                    case "filterwheel":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.FilterWheel));
                        return;

                    case "guider":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Guider));
                        return;

                    case "dome":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Dome));
                        return;

                    case "rotator":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Rotator));
                        return;

                    case "safetymonitor":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.SafetyMonitor));
                        return;

                    case "flatdevice":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.FlatDevice));
                        return;

                    case "switch":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Switch));
                        return;

                    case "image":
                        HttpContext.WriteToResponse(await EquipmentMediator.GetImage(int.Parse(parameter), int.Parse(index)));
                        return;

                    case "thumbnail":
                        HttpContext.WriteToResponse(await EquipmentMediator.GetThumbnail(int.Parse(parameter), int.Parse(index)));
                        return;

                    case "weather":
                        HttpContext.WriteToResponse(EquipmentMediator.GetDeviceInfo(EquipmentType.Weather));
                        return;

                    default:
                        HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
            }
        }

        #endregion

        #region SET

        [Route(HttpVerbs.Post, "/equipment")]
        public async Task SetEquipment()
        {
            POSTData data = await HttpContext.GetRequestDataAsync<POSTData>();
            if (string.IsNullOrEmpty(data.Device))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            if (string.IsNullOrWhiteSpace(data.Device) || string.IsNullOrWhiteSpace(data.Action))
            {
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable("POST Header not valid"));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (data.Device)
                {
                    case "camera":
                        HttpContext.WriteToResponse(await EquipmentController.Camera(data));
                        return;

                    case "telescope":
                        HttpContext.WriteToResponse(await EquipmentController.Telescope(data));
                        return;

                    case "focuser":
                        HttpContext.WriteToResponse(await EquipmentController.Focuser(data));
                        return;

                    case "rotator":
                        HttpContext.WriteToResponse(await EquipmentController.Rotator(data));
                        return;

                    case "filterwheel":
                        HttpContext.WriteToResponse(await EquipmentController.FilterWheel(data));
                        return;

                    case "dome":
                        HttpContext.WriteToResponse(await EquipmentController.Dome(data));
                        return;

                    case "switch":
                        HttpContext.WriteToResponse(await EquipmentController.Switch(data));
                        return;

                    case "guider":
                        HttpContext.WriteToResponse(await EquipmentController.Guider(data));
                        return;

                    case "flatdevice":
                        HttpContext.WriteToResponse(await EquipmentController.FlatDevice(data));
                        return;

                    case "safetymonitor":
                        HttpContext.WriteToResponse(await EquipmentController.SafetyMonitor(data));
                        return;

                    case "sequence":
                        HttpContext.WriteToResponse(EquipmentController.Sequence(data));
                        return;

                    case "application":
                        HttpContext.WriteToResponse(EquipmentController.Application(data));
                        return;

                    default:
                        HttpContext.WriteToResponse(CoreUtility.CreateErrorTable("Device not found"));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
            }
        }

        [Route(HttpVerbs.Post, "/profile")]
        public async Task SetProfile()
        {
            POSTData data = await HttpContext.GetRequestDataAsync<POSTData>();
            if (string.IsNullOrEmpty(data.Device))
            {
                Logger.Error(PROPERTY_NOT_SEND);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(PROPERTY_NOT_SEND));
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {

                switch (data.Device)
                {
                    case "switch":
                        HttpContext.WriteToResponse(EquipmentController.SwitchProfile(data));
                        return;
                    case "change-value":
                        HttpContext.WriteToResponse(EquipmentController.ChangeProfileValue(data));
                        return;
                    default:
                        HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(INVALID_PROPERTY));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(ex.Message));
            }
        }

        #endregion
    }
}