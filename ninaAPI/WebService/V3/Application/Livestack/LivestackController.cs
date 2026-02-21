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
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Model;
using ninaAPI.WebService.V2;
using ninaAPI.WebService.V3.Service;

namespace ninaAPI.WebService.V3.Application.Livestack
{
    public class LivestackController : WebApiController
    {
        private readonly IMessageBroker messageBroker;
        private readonly IProfileService profileService;
        private readonly ResponseHandler responseHandler;

        public LivestackController(IMessageBroker messageBroker, IProfileService profileService, ResponseHandler responseHandler)
        {
            this.messageBroker = messageBroker;
            this.profileService = profileService;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/status")]
        public async Task GetLivestackStatus()
        {
            await responseHandler.SendObject(HttpContext, new StringResponse(LivestackWatcher.LivestackStatus));
        }

        [Route(HttpVerbs.Post, "/start")]
        public async Task StartLivestack()
        {
            await messageBroker.Publish(new LiveStackMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StartLiveStack", string.Empty));
            await responseHandler.SendObject(HttpContext, new StringResponse("Live stack started"));
        }

        [Route(HttpVerbs.Post, "/stop")]
        public async Task StopLivestack()
        {
            await messageBroker.Publish(new LiveStackMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StopLiveStack", string.Empty));
            await responseHandler.SendObject(HttpContext, new StringResponse("Live stack stopped"));
        }

        [Route(HttpVerbs.Get, "/image")]
        public async Task GetLivestackImageAvailable()
        {
            await responseHandler.SendObject(HttpContext, LiveStackWatcher.LiveStackHistory.Images.Select(x => new
            {
                x.BlueStackCount,
                x.Filter,
                x.GreenStackCount,
                x.IsMonochrome,
                x.RedStackCount,
                x.StackCount,
                x.Target
            }));
        }

        [Route(HttpVerbs.Get, "/image/{target}/{filter}")]
        public async Task GetLivestackImage(string target, string filter)
        {
            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet parameters = ImageQueryParameterSet.ByProfile(profileService.ActiveProfile);
            parameters.Evaluate(HttpContext);

            BitmapSource image = LiveStackWatcher.LiveStackHistory.GetLast(filter, target) ?? throw new HttpException(HttpStatusCode.NotFound, "No image with specified filter and target found");

            image = ImageService.ResizeBitmap(image, parameters);
            ImageWriter writer = ImageWriter.GetImageWriter(image, parameters.Format.Value);

            await responseHandler.SendBytes(HttpContext, writer.Encode(parameters.Quality.Value), writer.MimeType);
        }
    }
}