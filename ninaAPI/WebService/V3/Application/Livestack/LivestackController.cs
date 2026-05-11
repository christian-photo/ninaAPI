#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.Model;
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3.Service;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Livestack
{
    public class LivestackController : IHttpController
    {
        private readonly IMessageBroker messageBroker;
        private readonly IProfileService profileService;

        public LivestackController(IMessageBroker messageBroker, IProfileService profileService)
        {
            this.messageBroker = messageBroker;
            this.profileService = profileService;
        }

        public object GetLivestackStatus()
        {
            return new { IsRunning = LivestackWatcher.IsLivestackRunning };
        }

        public async Task<StringResponse> StartLivestack()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StartLiveStack", string.Empty));
            return new StringResponse("Live stack started");
        }

        public async Task<StringResponse> StopLivestack()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StopLiveStack", string.Empty));
            return new StringResponse("Live stack stopped");
        }

        public object GetLivestackImageAvailable()
        {
            return LiveStackWatcher.LiveStackHistory.Images.Select(x => new
            {
                x.BlueStackCount,
                x.Filter,
                x.GreenStackCount,
                x.IsMonochrome,
                x.RedStackCount,
                x.StackCount,
                x.Target
            });
        }

        public async Task GetLivestackImage(string target, string filter, HttpSession session)
        {
            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet parameters = ImageQueryParameterSet.ByProfile(profileService.ActiveProfile);
            parameters.Evaluate(session.Request);

            BitmapSource image = LiveStackWatcher.LiveStackHistory.GetLast(filter, target) ?? throw new HttpException(HttpStatusCode.NotFound, "No image with specified filter and target found");

            image = ImageService.ResizeBitmap(image, parameters);
            ImageWriter writer = ImageWriter.GetImageWriter(image, parameters.Format.Value);

            await session.Response.Body(writer.Encode(parameters.Quality.Value), writer.MimeType).SendAsync();
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/status", () => GetLivestackStatus());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/start", async () => await StartLivestack());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/stop", async () => await StopLivestack());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/image", () => GetLivestackImageAvailable());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/image/:target/:filter", async (HttpSession session, string target, string filter) => await GetLivestackImage(target, filter, session));
        }
    }
}