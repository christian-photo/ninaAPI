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
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Model;
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3.Service;
using SimpleW;
using SimpleW.Service.BasicAuth;

namespace ninaAPI.WebService.V3.Application.Livestack
{
    [Route("/v3/api/livestack")]
    [BasicAuth]
    public class LivestackController : Controller
    {
        private readonly IMessageBroker messageBroker;
        private readonly IProfileService profileService;

        public LivestackController(IMessageBroker messageBroker, IProfileService profileService)
        {
            this.messageBroker = messageBroker;
            this.profileService = profileService;
        }

        [Route("GET", "/status")]
        public object GetLivestackStatus()
        {
            return new { IsRunning = LivestackWatcher.IsLivestackRunning };
        }

        [Route("POST", "/start")]
        public async Task<StringResponse> StartLivestack()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StartLiveStack", string.Empty));
            return new StringResponse("Live stack started");
        }

        [Route("POST", "/stop")]
        public async Task<StringResponse> StopLivestack()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StopLiveStack", string.Empty));
            return new StringResponse("Live stack stopped");
        }

        [Route("GET", "/image")]
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

        [Route("GET", "/image/:target/:filter")]
        public async Task GetLivestackImage(string target, string filter)
        {
            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet parameters = ImageQueryParameterSet.ByProfile(profileService.ActiveProfile);
            parameters.Evaluate(Request);

            BitmapSource image = LiveStackWatcher.LiveStackHistory.GetLast(filter, target) ?? throw new HttpException(HttpStatusCode.NotFound, "No image with specified filter and target found");

            image = ImageService.ResizeBitmap(image, parameters);
            ImageWriter writer = ImageWriter.GetImageWriter(image, parameters.Format.Value);

            await Response.Body(writer.Encode(parameters.Quality.Value), writer.MimeType).SendAsync();
        }
    }
}