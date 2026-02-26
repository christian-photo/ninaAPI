#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Profile;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Application.Profile
{
    public class ProfileController : WebApiController
    {
        private readonly IProfileService profileService;
        private readonly ResponseHandler responseHandler;

        public ProfileController(
            IProfileService profileService,
            ResponseHandler responseHandler)
        {
            this.profileService = profileService;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task GetActiveProfileMeta()
        {
            var active = profileService.Profiles.First(x => x.Id == profileService.ActiveProfile.Id);

            await responseHandler.SendObject(HttpContext, active);
        }

        [Route(HttpVerbs.Get, "/list")]
        public async Task GetProfileList()
        {
            await responseHandler.SendObject(HttpContext, profileService.Profiles);
        }

        [Route(HttpVerbs.Get, "/settings")]
        public async Task GetActiveProfileSettings()
        {
            // Maybe a bit verbose like this, also the filters are not using the api format, could be worth checking
            await responseHandler.SendObject(HttpContext, profileService.ActiveProfile);
        }

        [Route(HttpVerbs.Get, "/horizon")]
        public async Task GetProfileHorizon()
        {
            await responseHandler.SendObject(HttpContext, new HorizonResponse(AdvancedAPI.Controls.Profile.ActiveProfile.AstrometrySettings.Horizon));
        }

        [Route(HttpVerbs.Put, "/")]
        public async Task ChangeProfile()
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(HttpContext);

            ProfileMeta targetProfile = profileService.Profiles.FirstOrDefault(x => x.Id == idParameter.Value);
            if (targetProfile is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Profile with specified id not found");
            }

            bool success = profileService.SelectProfile(targetProfile);
            await responseHandler.SendObject(HttpContext, new StringResponse(success ? "Profile changed" : "Profile change failed"), success ? 200 : 500);
        }

        [Route(HttpVerbs.Patch, "/")]
        public async Task UpdateProfileValue([JsonData] ProfileValueChangeConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            string[] pathSplit = config.PathDescription.Split('-'); // e.g. 'CameraSettings-PixelSize' -> CameraSettings, PixelSize
            object position = AdvancedAPI.Controls.Profile.ActiveProfile;

            if (pathSplit.Length == 1)
            {
                var prop = position.GetType().GetProperty(config.PathDescription);
                // This is needed because (as an example) Newtonsoft.JSON by default deserializes to double, and an assignment to a float would fail
                var converted = Convert.ChangeType(config.Value, prop.PropertyType);
                prop.SetValue(position, converted);
            }
            else
            {
                for (int i = 0; i <= pathSplit.Length - 2; i++)
                {
                    if (IsIndexable(position, out Type indexType, out PropertyInfo indexProp))
                    {
                        if (indexType == typeof(string))
                        {
                            indexProp.GetValue(position, [pathSplit[i]]);
                        }
                        else
                        {
                            position = indexProp.GetValue(position, [int.Parse(pathSplit[i])]);
                        }
                    }
                    else
                    {
                        position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
                    }
                }
                PropertyInfo prop = position.GetType().GetProperty(pathSplit[^1]);
                // This is needed because (as an example) Newtonsoft.JSON by default deserializes to double, and an assignment to a float would fail
                var converted = Convert.ChangeType(config.Value, prop.PropertyType);
                prop.SetValue(position, converted);
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Value was updated"));
        }

        private static bool IsIndexable(object obj, out Type indexType, out PropertyInfo indexProp)
        {
            indexProp = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.GetIndexParameters().Length > 0, null);
            if (indexProp == null)
            {
                indexType = null;
                return false;
            }

            indexType = indexProp.GetIndexParameters()[0].ParameterType;
            return true;
        }

        [Route(HttpVerbs.Post, "/")]
        public async Task CreateProfile()
        {
            profileService.Add();
            await responseHandler.SendObject(HttpContext, profileService.Profiles.Last());
        }

        [Route(HttpVerbs.Post, "/clone")]
        public async Task CloneProfile()
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(HttpContext);

            ProfileMeta targetProfile = profileService.Profiles.FirstOrDefault(x => x.Id == idParameter.Value);
            if (targetProfile is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Profile with specified id not found");
            }

            bool success = profileService.Clone(targetProfile);
            await responseHandler.SendObject(HttpContext, new StringResponse(success ? "Profile cloned" : "Profile clone failed"), success ? 200 : 500);
        }

        [Route(HttpVerbs.Delete, "/")]
        public async Task DeleteProfile()
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(HttpContext);

            ProfileMeta targetProfile = profileService.Profiles.FirstOrDefault(x => x.Id == idParameter.Value);
            if (targetProfile is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Profile with specified id not found");
            }

            if (targetProfile.Id == profileService.ActiveProfile.Id)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Cannot delete active profile");
            }

            bool success = profileService.RemoveProfile(targetProfile);
            await responseHandler.SendObject(HttpContext, new StringResponse(success ? "Profile deleted" : "Profile delete failed"), success ? 200 : 500);
        }
    }

    public class ProfileValueChangeConfig
    {
        // Again in the format CameraSettings-PixelSize
        [Required(AllowEmptyStrings = false)]
        public string PathDescription { get; set; }

        [Required]
        public object Value { get; set; }
    }
}
