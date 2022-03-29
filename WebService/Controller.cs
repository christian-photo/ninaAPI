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

        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return "ninaAPI: https://github.com/rennmaus-coder/ninaAPI/wiki";
        }

        #region GET

        [Route(HttpVerbs.Get, "/get/history/count")]
        public Hashtable GetHistoryCount()
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return FAILED_TABLE;
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return FAILED_TABLE;

            Logger.Info($"API call: api/get/history/count");
            try
            {
                return EquipmentMediator.GetImageCount();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return FAILED_TABLE;
        }

        [Route(HttpVerbs.Get, "/get/history/{id}")]
        public List<Hashtable> GetHistory(string id)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return new List<Hashtable>() { FAILED_TABLE};
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return new List<Hashtable>() { FAILED_TABLE};

            Logger.Info($"API call: api/get/history/{id}");
            try
            {
                return EquipmentMediator.GetImageHistory(id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new List<Hashtable>() { FAILED_TABLE };
        }

        [Route(HttpVerbs.Get, "/get/profile/{id}")]
        public List<Hashtable> GetProfile(string id)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return new List<Hashtable>() { FAILED_TABLE };
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return new List<Hashtable>() { FAILED_TABLE };

            Logger.Info($"API call: api/get/profile/{id}");
            try
            {
                return EquipmentMediator.GetProfile(id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new List<Hashtable>() { FAILED_TABLE };
        }

        [Route(HttpVerbs.Get, "/get/sequence/count")]
        public Hashtable GetSequenceCount()
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return FAILED_TABLE;
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return FAILED_TABLE;

            Logger.Info($"API call: api/get/sequence/count");
            try
            {
                return EquipmentMediator.GetSequenceCount();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return FAILED_TABLE;
        }

        [Route(HttpVerbs.Get, "/get/sequence/{action}")]
        public List<Hashtable> GetSequence(string action)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return new List<Hashtable>() { FAILED_TABLE };
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return new List<Hashtable>() { FAILED_TABLE };
            
            Logger.Info($"API call: api/get/sequence/{action}");
            try
            {
                return EquipmentMediator.GetSequence(action.ToLower());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new List<Hashtable>() { FAILED_TABLE };
        }

        [Route(HttpVerbs.Get, "/get/{resource}/{action}")]
        public Hashtable GetInformation(string resource, string action)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return FAILED_TABLE;
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return FAILED_TABLE;
            
            Logger.Info($"API call: api/get/{resource}/{action}");
            try
            {
                switch (resource.ToLower())
                {
                    case "camera":
                        return EquipmentMediator.GetCamera(action.ToLower());

                    case "telescope":
                        return EquipmentMediator.GetTelescope(action.ToLower());

                    case "focuser":
                        return EquipmentMediator.GetFocuser(action.ToLower());

                    case "filterwheel":
                        return EquipmentMediator.GetFilterWheel(action.ToLower());

                    case "guider":
                        return EquipmentMediator.GetGuider(action.ToLower());

                    case "dome":
                        return EquipmentMediator.GetDome(action.ToLower());

                    case "rotator":
                        return EquipmentMediator.GetRotator(action.ToLower());

                    case "safetymonitor":
                        return EquipmentMediator.GetSafetyMonitor(action.ToLower());

                    case "flatdevice":
                        return EquipmentMediator.GetFlatDevice(action.ToLower());

                    case "switch":
                        return EquipmentMediator.GetSwitch(action.ToLower());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return FAILED_TABLE;
        }

        #endregion

        #region SET

        [Route(HttpVerbs.Get, "/set/{resource}/{action}")]
        public async Task<Hashtable> SetEquipment(string resource, string action)
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!CheckKey(apiKey)) return FAILED_TABLE;
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null) return FAILED_TABLE;

            Logger.Info($"API call: api/set/{resource}/{action}");
            try
            {
                switch (resource.ToLower())
                {
                    case "camera":
                        return await EquipmentController.Camera(action.ToLower());

                    case "telescope":
                        return await EquipmentController.Telescope(action.ToLower());

                    case "focuser":
                        return await EquipmentController.Focuser(action.ToLower());

                    case "rotator":
                        return await EquipmentController.Rotator(action.ToLower());

                    case "filterwheel":
                        return await EquipmentController.FilterWheel(action.ToLower());

                    case "dome":
                        return await EquipmentController.Dome(action.ToLower());

                    case "switch":
                        return await EquipmentController.Switch(action.ToLower());

                    case "guider":
                        return await EquipmentController.Guider(action.ToLower());

                    case "flatdevice":
                        return await EquipmentController.FlatDevice(action.ToLower());

                    case "safteymonitor":
                        return await EquipmentController.SafteyMonitor(action.ToLower());

                    case "sequence":
                        return await EquipmentController.Sequence(action.ToLower());

                    case "application":
                        return await EquipmentController.Application(action.ToLower());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return FAILED_TABLE;
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