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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using NINA.Profile;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using SimpleW;
using SimpleW.Service.BasicAuth;

namespace ninaAPI.WebService.V3.Application.Profile
{
    [Route("/v3/api/profile")]
    [BasicAuth]
    public class ProfileController : Controller
    {
        private readonly IProfileService profileService;
        private readonly ISerializerService serializer;

        public ProfileController(
            IProfileService profileService,
            ISerializerService serializer)
        {
            this.profileService = profileService;
            this.serializer = serializer;
        }

        [Route("GET", "/")]
        public ProfileMeta GetActiveProfileMeta()
        {
            return profileService.Profiles.First(x => x.IsActive);
        }

        [Route("GET", "/list")]
        public IList<ProfileMeta> GetProfileList()
        {
            return profileService.Profiles;
        }

        [Route("GET", "/settings")]
        public ProfileDTO GetActiveProfileSettings()
        {
            return new ProfileDTO(profileService.ActiveProfile);
        }

        [Route("GET", "/horizon")]
        public HorizonResponse GetProfileHorizon()
        {
            return new HorizonResponse(profileService.ActiveProfile.AstrometrySettings.Horizon);
        }

        [Route("PUT", "/")]
        public StringResponse ChangeProfile()
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(Request);

            ProfileMeta targetProfile = profileService.Profiles.FirstOrDefault(x => x.Id == idParameter.Value);
            if (targetProfile is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Profile with specified id not found");
            }

            if (!profileService.SelectProfile(targetProfile))
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Profile change failed");
            }
            return new StringResponse("Profile changed");
        }

        [Route("PATCH", "/settings")]
        public StringResponse UpdateProfileValue()
        {
            ProfileValueChangeConfig config = serializer.Deserialize<ProfileValueChangeConfig>(Request.BodyString);
            Validator.ValidateObject(config, new ValidationContext(config));

            ReflectionHelper.SetValueReflected(AdvancedAPI.Controls.Profile.ActiveProfile, config.PathDescription, config.Value);

            return new StringResponse("Value was updated");
        }

        [Route("POST", "/")]
        public ProfileMeta CreateProfile()
        {
            profileService.Add();
            return profileService.Profiles.Last();
        }

        [Route("POST", "/clone")]
        public StringResponse CloneProfile()
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(Request);

            ProfileMeta targetProfile = profileService.Profiles.FirstOrDefault(x => x.Id == idParameter.Value);
            if (targetProfile is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Profile with specified id not found");
            }

            if (!profileService.Clone(targetProfile))
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Profile clone failed");
            }
            return new StringResponse("Profile cloned");
        }

        [Route("DELETE", "/")]
        public StringResponse DeleteProfile()
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(Request);

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
            return new StringResponse("Profile deleted");
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
