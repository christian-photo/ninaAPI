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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.FileFormat.FITS;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Equipment.Camera;
using ninaAPI.WebService.V3.Model;
using ninaAPI.WebService.V3.Service;

namespace ninaAPI.WebService.V3.Application.Image
{
    public class ImageController : WebApiController
    {
        private readonly IImageDataFactory imageDataFactory;
        private readonly IProfileService profileService;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly ICameraMediator cameraMediator;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ResponseHandler responseHandler;

        public ImageController(IImageDataFactory imageDataFactory,
            IProfileService profileService,
            IPlateSolverFactory plateSolverFactory,
            ICameraMediator cameraMediator,
            IApplicationStatusMediator statusMediator,
            ResponseHandler responseHandler)
        {
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.statusMediator = statusMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/{index}")]
        public async Task GetImage(int index)
        {
            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", CameraController.FindBayer(profile, cameraMediator), false);

            imageQuery.Evaluate(HttpContext);
            imageTypeParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);
            ImageWriter writer = await ImageService.ProcessAndPrepareImage(p.GetPath(), p.IsBayered, imageQuery, p.BitDepth);

            HttpContext.Response.ContentType = writer.MimeType;
            writer.WriteToStream(imageQuery, HttpContext.OpenResponseStream());
        }

        [Route(HttpVerbs.Get, "/{index}/thumbnail")]
        public async Task GetThumbnail(int index)
        {
            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);

            if (!File.Exists(p.GetThumbnailPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Thumbnail does not exist");
            }

            HttpContext.Response.ContentType = "image/png";
            using (FileStream image = File.OpenRead(p.GetThumbnailPath()))
            {
                await image.CopyToAsync(HttpContext.OpenResponseStream());
            }
        }

        [Route(HttpVerbs.Get, "/{index}/raw")]
        public async Task GetImageRaw(int index)
        {
            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);

            if (!File.Exists(p.GetPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image does not exist");
            }

            if (p.GetPath().EndsWith(".fits", true, null))
            {
                var imageData = await Retry.Do(
                    async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(
                        p.GetPath(),
                        16,
                        p.IsBayered,
                        RawConverterEnum.FREEIMAGE
                    ), TimeSpan.FromMilliseconds(200), 10
                );

                // Create the FITS image.
                FITS f = new FITS(
                    imageData.Data.FlatArray,
                    imageData.Properties.Width,
                    imageData.Properties.Height
                );

                f.PopulateHeaderCards(imageData.MetaData);

                HttpContext.Response.ContentType = "application/octet-stream";
                f.Write(HttpContext.OpenResponseStream());
            }
            else
            {
                var file = File.OpenRead(p.GetPath());
                HttpContext.Response.ContentType = "application/octet-stream";
                file.CopyTo(HttpContext.OpenResponseStream());
                file.Close();
            }
        }

        [Route(HttpVerbs.Patch, "/{index}/prefix")]
        public async Task AddPrefix(int index)
        {
            QueryParameter<string> prefixParameter = new QueryParameter<string>("prefix", "", true, (prefix) => prefix.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(HttpContext);
            prefixParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);

            string oldPath = p.GetPath();
            string newPath = Path.Join(Path.GetDirectoryName(oldPath), prefixParameter.Value + Path.GetFileName(oldPath));

            if (!File.Exists(p.GetPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image does not exist");
            }

            if (!File.Exists(newPath))
            {
                File.Move(p.GetPath(), newPath);
                p.SetPath(newPath);
            }

            // Not sure how to treat the action if the file already exist, so we just return a success

            await responseHandler.SendObject(HttpContext, new
            {
                OldFilename = Path.GetFileName(oldPath),
                NewFilename = Path.GetFileName(newPath)
            });
        }

        [Route(HttpVerbs.Post, "/{index}/platesolve")]
        public async Task ImageSolve(int index, [JsonData] PlatesolveConfig config)
        {
            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);
            if (!File.Exists(p.GetPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image does not exist");
            }

            Coordinates coordinates = config.Coordinates.ToCoordinates();
            var result = await new PlateSolveService(
                imageDataFactory,
                plateSolverFactory,
                profile.PlateSolveSettings,
                statusMediator)
                .PlateSolve(
                    p.GetPath(),
                    config,
                    (double)config.PixelSize,
                    coordinates,
                    HttpContext.CancellationToken,
                    p.BitDepth,
                    p.IsBayered);

            await responseHandler.SendObject(HttpContext, result);
        }

        [Route(HttpVerbs.Get, "/prepared")]
        public async Task GetPreparedImage()
        {
            IProfile profile = profileService.ActiveProfile;

            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", CameraController.FindBayer(profile, cameraMediator), false);

            imageQuery.Evaluate(HttpContext);

            if (ImageWatcher.PreparedImage is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "No image prepared");
            }

            ImageWriter writer = await ImageService.ProcessAndPrepareImage(ImageWatcher.PreparedImage, imageQuery);

            HttpContext.Response.ContentType = writer.MimeType;
            writer.WriteToStream(imageQuery, HttpContext.OpenResponseStream());
        }

        [Route(HttpVerbs.Post, "/prepared/platesolve")]
        public async Task PreparedImageSolve([JsonData] PlatesolveConfig config)
        {
            IProfile profile = profileService.ActiveProfile;

            if (ImageWatcher.PreparedImage is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "No image prepared");
            }

            Coordinates coordinates = config.Coordinates.ToCoordinates();
            var result = await new PlateSolveService(
                imageDataFactory,
                plateSolverFactory,
                profile.PlateSolveSettings,
                statusMediator)
                .PlateSolve(
                    ImageWatcher.PreparedImage.RawImageData,
                    config,
                    (double)config.PixelSize,
                    coordinates,
                    HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, result);
        }

        [Route(HttpVerbs.Get, "/history")]
        public async Task GetImageHistory()
        {
            PagerParameterSet pagerParameterSet = PagerParameterSet.Default();
            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(HttpContext);
            pagerParameterSet.Evaluate(HttpContext);

            IEnumerable<ImageResponse> history = ImageWatcher.GetImageHistory();

            history = !imageTypeParameter.WasProvided ? history : history.Where(x => x.ImageType.Equals(imageTypeParameter.Value));

            if (!history.Any())
            {
                throw new HttpException(HttpStatusCode.NotFound, "No images available");
            }
            var result = new Pager<ImageResponse>([.. history]).GetPage(pagerParameterSet.PageParameter.Value, pagerParameterSet.PageSizeParameter.Value);

            await responseHandler.SendObject(HttpContext, result);
        }

        private static ImageResponse GetImageResponseFromHistory(int index, QueryParameter<string> imageType)
        {
            IEnumerable<ImageResponse> history = ImageWatcher.GetImageHistory();

            history = !imageType.WasProvided ? history : history.Where(x => x.ImageType.Equals(imageType.Value));

            if (!history.Any())
            {
                throw new HttpException(HttpStatusCode.NotFound, "No images available");
            }
            else if (!index.IsBetween(0, history.Count() - 1))
            {
                throw CommonErrors.ParameterOutOfRange(nameof(index), 0, history.Count() - 1);
            }

            return history.ElementAt(index);
        }
    }
}
