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
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.SkySurvey;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.V3.Service;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Framing
{
    [Route("/v3/api/framing")]
    public class FramingController : Controller
    {
        private readonly IFramingAssistantVM framingVM;
        private readonly ICameraMediator camera;
        private readonly IProfileService profileService;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        public FramingController(IFramingAssistantVM framingVm, ICameraMediator camera, IProfileService profileService, ApiProcessMediator processMediator, ISerializerService serializer)
        {
            this.framingVM = framingVm;
            this.camera = camera;
            this.profileService = profileService;
            this.processMediator = processMediator;
            this.serializer = serializer;
        }

        [Route("GET", "/")]
        public FramingInfoContainer FramingInfo()
        {
            return new FramingInfoContainer(framingVM);
        }

        [Route("GET", "/image")]
        public async Task GetImage()
        {
            IProfile profile = profileService.ActiveProfile;

            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            imageQuery.Size.AllowOneSide = false; // both sides must be provided together, because there is no fixed aspect ratio
            QueryParameter<double> raParameter = new QueryParameter<double>("ra", 0, true, (ra) => ra.IsBetween(-180, 180));
            QueryParameter<double> decParameter = new QueryParameter<double>("dec", 0, true, (dec) => dec.IsBetween(-90, 90));
            QueryParameter<Epoch> epochParameter = new QueryParameter<Epoch>("epoch", Epoch.J2000, false);
            QueryParameter<double> fovParameter = new QueryParameter<double>("fov", 10, false, (fov) => fov > 0);
            QueryParameter<SkySurveySource> sourceParameter = new QueryParameter<SkySurveySource>("source", SkySurveySource.CACHE, true);

            imageQuery.Evaluate(Request);
            var source = sourceParameter.Get(Request);
            var ra = raParameter.Get(Request);
            var fov = fovParameter.Get(Request);
            var dec = decParameter.Get(Request);
            var epoch = epochParameter.Get(Request);

            var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), epoch);

            BitmapSource imageSource = null;

            if (source == SkySurveySource.CACHE)
            {
                // TODO: Find out why the individual images are so low res
                string framingCache = profile.ApplicationSettings.SkySurveyCacheDirectory;
                CacheSkySurveyImageFactory factory = new CacheSkySurveyImageFactory(imageQuery.Size.Value.Width, imageQuery.Size.Value.Height, framingVM.Cache);
                imageSource = factory.Render(coordinates, fov, 0);
            }
            else
            {
                if (source != framingVM.FramingAssistantSource) framingVM.FramingAssistantSource = source;
                if (fov != framingVM.FieldOfView && fovParameter.WasProvided) framingVM.FieldOfView = fov;

                if (!await framingVM.SetCoordinates(new DeepSkyObject("api request", coordinates, profile.AstrometrySettings.Horizon)))
                {
                    throw new HttpException(HttpStatusCode.InternalServerError, "Error while loading image");
                }
                // Resizing is unnessary when loading from cache because it is automatically rendered at the right resolution
                imageSource = ImageService.ResizeBitmap(framingVM.ImageParameter.Image, imageQuery);
            }

            ImageWriter writer = ImageWriter.GetImageWriter(imageSource, imageQuery.Format.Value);

            await Response.Body(writer.Encode(imageQuery.Quality.Value), writer.MimeType).SendAsync();
        }

        [Route("PATCH", "/")]
        public async Task<FramingInfoContainer> FramingUpdate()
        {
            FramingUpdate config = serializer.Deserialize<FramingUpdate>(Request.BodyString);
            Validator.ValidateObject(config, new ValidationContext(config)); // is there a better way to do this?

            if (config.BoundHeight != null) framingVM.BoundHeight = config.BoundHeight.Value;
            if (config.BoundWidth != null) framingVM.BoundWidth = config.BoundWidth.Value;
            if (config.CameraHeight != null) framingVM.CameraHeight = config.CameraHeight.Value;
            if (config.CameraWidth != null) framingVM.CameraWidth = config.CameraWidth.Value;
            if (config.CameraPixelSize != null) framingVM.CameraPixelSize = config.CameraPixelSize.Value;
            if (!string.IsNullOrEmpty(config.DSOName)) framingVM.DSO.Name = config.DSOName;
            if (config.FieldOfView != null) framingVM.FieldOfView = config.FieldOfView.Value;
            if (config.FocalLength != null) framingVM.FocalLength = config.FocalLength.Value;
            if (config.HorizontalPanels != null) framingVM.HorizontalPanels = config.HorizontalPanels.Value;
            if (config.VerticalPanels != null) framingVM.VerticalPanels = config.VerticalPanels.Value;
            if (config.FramingSource != null) framingVM.FramingAssistantSource = config.FramingSource.Value;

            if (config.Coordinates != null)
            {
                await framingVM.SetCoordinates(new DeepSkyObject(framingVM.DSO.Name, config.Coordinates.ToCoordinates(), profileService.ActiveProfile.AstrometrySettings.Horizon));
            }

            return FramingInfo();
        }

        // I dont copy the slew endopint because you can use the mount slew as well
        [Route("POST", "/solve-rotation")]
        public object FramingSolveRotation()
        {
            if (!framingVM.RectangleCalculated)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Framing is not ready");
            }
            else if (!camera.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (camera.GetInfo().IsExposing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera is currently exposing");
            }
            else if (!camera.IsFreeToCapture(framingVM))
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera in use");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await (Task<bool>)framingVM.GetType().GetMethod("GetRotationFromCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(framingVM, [null]),
                ApiProcessType.FramingSolveRotation
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }
    }
}
