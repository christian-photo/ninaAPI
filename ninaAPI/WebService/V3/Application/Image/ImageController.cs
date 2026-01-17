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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.FileFormat.FITS;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Equipment.Camera;
using ninaAPI.WebService.V3.Model;
using ninaAPI.WebService.V3.Service;

namespace ninaAPI.WebService.V3.Application.Image
{
    public class ImageController : WebApiController
    {
        private IImageDataFactory imageDataFactory;
        private IProfileService profileService;
        private IPlateSolverFactory plateSolverFactory;
        private ICameraMediator cameraMediator;
        private ApiProcessMediator apiProcessMediator;
        private ResponseHandler responseHandler;

        public ImageController(IImageDataFactory imageDataFactory,
            IProfileService profileService,
            IPlateSolverFactory plateSolverFactory,
            ICameraMediator cameraMediator,
            ApiProcessMediator apiProcessMediator,
            ResponseHandler responseHandler)
        {
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.apiProcessMediator = apiProcessMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/image/{index}")]
        public async Task GetImage(int index)
        {
            IProfile profile = profileService.ActiveProfile;

            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", CameraController.FindBayer(profile, cameraMediator), false);

            imageQuery.Evaluate(HttpContext);
            imageTypeParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);
            BitmapEncoder encoder = await ImageService.ProcessAndPrepareImage(p.GetPath(), p.IsBayered, imageQuery); // TODO: See if its possible to save the bitdepth

            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                await responseHandler.SendBytes(HttpContext, memory.ToArray(), encoder.CodecInfo.MimeTypes);
            }
        }

        [Route(HttpVerbs.Get, "/image/{index}/raw")]
        public async Task GetImageRaw(int index)
        {
            QueryParameter<string> imageTypeParameter = new QueryParameter<string>("imageType", "", false, (type) => CoreUtility.IMAGE_TYPES.Contains(type));
            imageTypeParameter.Get(HttpContext);

            ImageResponse p = GetImageResponseFromHistory(index, imageTypeParameter);

            using MemoryStream fileStream = new MemoryStream();

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

                f.Write(fileStream);
            }
            else
            {
                var file = File.OpenRead(p.GetPath());
                file.CopyTo(fileStream);
                file.Close();
            }

            await responseHandler.SendBytes(HttpContext, fileStream.ToArray(), "application/octet-stream");
        }

        private ImageResponse GetImageResponseFromHistory(int index, QueryParameter<string> imageType)
        {
            IEnumerable<ImageResponse> points;
            lock (ImageWatcher.imageLock)
            {
                points = imageType.WasProvided ? ImageWatcher.Images : ImageWatcher.Images.Where(x => x.ImageType.Equals(imageType.Value));
            }

            if (!points.Any())
            {
                throw new HttpException(404, "No images available");
            }
            else if (!index.IsBetween(0, points.Count() - 1))
            {
                throw CommonErrors.ParameterOutOfRange(nameof(index), 0, points.Count() - 1);
            }

            return points.ElementAt(index);
        }
    }
}
