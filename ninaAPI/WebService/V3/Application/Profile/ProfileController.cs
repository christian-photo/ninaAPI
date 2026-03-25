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
using System.Threading.Tasks;
using NINA.Profile;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Profile
{
    public class ProfileController : IHttpController
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

        public ProfileMeta GetActiveProfileMeta()
        {
            return profileService.Profiles.First(x => x.IsActive);
        }

        public IList<ProfileMeta> GetProfileList()
        {
            return profileService.Profiles;
        }

        public ProfileDTO GetActiveProfileSettings()
        {
            return new ProfileDTO(profileService.ActiveProfile);
        }

        public HorizonResponse GetProfileHorizon()
        {
            return new HorizonResponse(profileService.ActiveProfile.AstrometrySettings.Horizon);
        }

        public StringResponse ChangeProfile(HttpSession session)
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(session.Request);

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

        public StringResponse UpdateProfileValue(ProfileValueChangeConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            CoreUtility.SetValueReflected(AdvancedAPI.Controls.Profile.ActiveProfile, config.PathDescription, config.Value);

            return new StringResponse("Value was updated");
        }

        public ProfileMeta CreateProfile()
        {
            profileService.Add();
            return profileService.Profiles.Last();
        }

        public StringResponse CloneProfile(HttpSession session)
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(session.Request);

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

        public StringResponse DeleteProfile(HttpSession session)
        {
            QueryParameter<Guid> idParameter = new QueryParameter<Guid>("id", Guid.Empty, true);
            idParameter.Get(session.Request);

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

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => GetActiveProfileMeta());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/list", () => GetProfileList());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/settings", () => GetActiveProfileSettings());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/horizon", () => GetProfileHorizon());
            server.Map(HttpVerbs.PUT.ToString(), prefix, (HttpSession session) => ChangeProfile(session));
            server.Map(HttpVerbs.PATCH.ToString(), $"{prefix}/settings", (HttpSession session) => UpdateProfileValue(serializer.Deserialize<ProfileValueChangeConfig>(session.Request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix, () => CreateProfile());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/clone", (HttpSession session) => CloneProfile(session));
            server.Map(HttpVerbs.DELETE.ToString(), prefix, (HttpSession session) => DeleteProfile(session));
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
