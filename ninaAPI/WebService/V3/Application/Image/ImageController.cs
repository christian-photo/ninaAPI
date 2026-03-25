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
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.FileFormat.FITS;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Equipment.Camera;
using ninaAPI.WebService.V3.Model;
using ninaAPI.WebService.V3.Service;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Image
{
    public class ImageController : IHttpController
    {
        private readonly IImageDataFactory imageDataFactory;
        private readonly IProfileService profileService;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator mount;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ISerializerService serializer;

        public ImageController(IImageDataFactory imageDataFactory,
            IProfileService profileService,
            IPlateSolverFactory plateSolverFactory,
            ICameraMediator cameraMediator,
            ITelescopeMediator mount,
            IApplicationStatusMediator statusMediator,
            ISerializerService serializer)
        {
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.mount = mount;
            this.statusMediator = statusMediator;
            this.serializer = serializer;
        }

        public async Task GetImage(int index, HttpSession session)
        {
            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", CameraController.FindBayer(profile, cameraMediator), false);

            imageQuery.Evaluate(session.Request);
            imageTypeParameter.Get(session.Request);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);
            ImageWriter writer = await ImageService.ProcessAndPrepareImage(p.GetPath(), p.IsBayered, imageQuery, p.BitDepth);

            await session.Response.Body(writer.Encode(imageQuery.Quality.Value), writer.MimeType).SendAsync();
        }

        public async Task GetThumbnail(int index, HttpSession session)
        {
            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(session.Request);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);

            if (!File.Exists(p.GetThumbnailPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Thumbnail does not exist");
            }

            await session.Response.Body(await File.ReadAllBytesAsync(p.GetThumbnailPath()), "image/png").SendAsync();
        }

        public async Task GetImageRaw(int index, HttpSession session)
        {
            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(session.Request);

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
                        p.BitDepth,
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

                using (var target = new MemoryStream())
                {
                    f.Write(target); // TODO: TEsting
                    await session.Response.Body(target.ToArray(), "application/octet-stream").SendAsync();
                }
            }
            else
            {
                await session.Response.Body(await File.ReadAllBytesAsync(p.GetPath()), "application/octet-stream").SendAsync();
            }
        }

        public object AddPrefix(int index, ImagePrefixBody body, HttpSession session)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (body.Prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Prefix contains invalid characters");
            }

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(session.Request);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);

            string oldPath = p.GetPath();
            string newPath = Path.Join(Path.GetDirectoryName(oldPath), body.Prefix + Path.GetFileName(oldPath));

            if (!File.Exists(p.GetPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image does not exist");
            }

            if (!File.Exists(newPath))
            {
                File.Move(p.GetPath(), newPath);
                p.SetPath(newPath);
            }
            else
            {
                throw new HttpException(HttpStatusCode.Conflict, "File already exists");
            }

            return new
            {
                OldFilename = Path.GetFileName(oldPath),
                NewFilename = Path.GetFileName(newPath)
            };
        }

        public async Task<PlateSolveResult> ImageSolve(int index, PlatesolveConfig config, HttpSession session)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(session.Request);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);
            if (!File.Exists(p.GetPath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image does not exist");
            }

            config.UpdateDefaults(profile, mount, cameraMediator);

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
                    session.RequestAborted,
                    profile,
                    p.BitDepth,
                    p.IsBayered);

            return result;
        }

        public async Task GetPreparedImage(HttpSession session)
        {
            IProfile profile = profileService.ActiveProfile;

            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", CameraController.FindBayer(profile, cameraMediator), false);

            imageQuery.Evaluate(session.Request);

            if (ImageWatcher.PreparedImage is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "No image prepared");
            }

            ImageWriter writer = await ImageService.ProcessAndPrepareImage(ImageWatcher.PreparedImage, imageQuery);

            await session.Response.Body(writer.Encode(imageQuery.Quality.Value), writer.MimeType).SendAsync();
        }

        public async Task<PlateSolveResult> PreparedImageSolve(PlatesolveConfig config, HttpSession session)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            IProfile profile = profileService.ActiveProfile;

            if (ImageWatcher.PreparedImage is null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "No image prepared");
            }

            config.UpdateDefaults(profile, mount, cameraMediator);

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
                    profile,
                    session.RequestAborted);

            return result;
        }

        public object GetImageHistory(HttpSession session)
        {
            PagerParameterSet pagerParameterSet = PagerParameterSet.Default();
            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(session.Request);
            pagerParameterSet.Evaluate(session.Request);

            IEnumerable<ImageResponse> history = ImageWatcher.GetImageHistory();

            history = !imageTypeParameter.WasProvided ? history : history.Where(x => x.ImageType.Equals(imageTypeParameter.Value));

            if (!history.Any())
            {
                throw new HttpException(HttpStatusCode.NotFound, "No images available");
            }
            var result = new Pager<ImageResponse>([.. history]).GetPage(pagerParameterSet.PageParameter.Value, pagerParameterSet.PageSizeParameter.Value);

            return result;
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

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix + "/:index", async (int index, HttpSession session) => await GetImage(index, session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/:index/thumbnail", async (int index, HttpSession session) => await GetThumbnail(index, session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/:index/raw", async (int index, HttpSession session) => await GetImageRaw(index, session));
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/:index/prefix", (int index, HttpSession session) => AddPrefix(index, serializer.Deserialize<ImagePrefixBody>(session.Request.BodyString), session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/:index/solve", async (int index, HttpSession session) => await ImageSolve(index, serializer.Deserialize<PlatesolveConfig>(session.Request.BodyString), session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/prepared", async (HttpSession session) => await GetPreparedImage(session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/prepared/solve", async (HttpSession session) => await PreparedImageSolve(serializer.Deserialize<PlatesolveConfig>(session.Request.BodyString), session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/history", (HttpSession session) => GetImageHistory(session));
        }
    }

    public class ImagePrefixBody
    {
        [Required]
        public string Prefix { get; set; }
    }
}
