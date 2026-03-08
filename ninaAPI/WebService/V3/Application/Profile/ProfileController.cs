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
using NINA.Equipment.Interfaces;
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
            var active = profileService.Profiles.First(x => x.IsActive);

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
            await responseHandler.SendObject(HttpContext, new ProfileDTO(profileService.ActiveProfile));
        }

        [Route(HttpVerbs.Get, "/horizon")]
        public async Task GetProfileHorizon()
        {
            await responseHandler.SendObject(HttpContext, new HorizonResponse(profileService.ActiveProfile.AstrometrySettings.Horizon));
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

            if (!profileService.SelectProfile(targetProfile))
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Profile change failed");
            }
            await responseHandler.SendObject(HttpContext, new StringResponse("Profile changed"));
        }

        [Route(HttpVerbs.Patch, "/settings")]
        public async Task UpdateProfileValue([JsonData] ProfileValueChangeConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            CoreUtility.SetValueReflected(AdvancedAPI.Controls.Profile.ActiveProfile, config.PathDescription, config.Value);

            await responseHandler.SendObject(HttpContext, new StringResponse("Value was updated"));
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

            if (!profileService.Clone(targetProfile))
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Profile clone failed");
            }
            await responseHandler.SendObject(HttpContext, new StringResponse("Profile cloned"));
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

            if (!profileService.RemoveProfile(targetProfile))
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Profile delete failed");
            }
            await responseHandler.SendObject(HttpContext, new StringResponse("Profile deleted"));
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
