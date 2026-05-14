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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
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

        // TODO: Test this endpoint
        [Route("GET", "/image")]
        public async Task GetImage()
        {
            IProfile profile = profileService.ActiveProfile;

            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile);
            QueryParameter<double> raParameter = new QueryParameter<double>("ra", 0, true, (ra) => ra.IsBetween(-180, 180));
            QueryParameter<double> decParameter = new QueryParameter<double>("dec", 0, true, (dec) => dec.IsBetween(-90, 90));
            QueryParameter<Epoch> epochParameter = new QueryParameter<Epoch>("epoch", Epoch.J2000, false);
            QueryParameter<SkySurveySource> sourceParameter = new QueryParameter<SkySurveySource>("source", SkySurveySource.CACHE, false);

            imageQuery.Evaluate(Request);
            var source = sourceParameter.Get(Request);
            var ra = raParameter.Get(Request);
            var dec = decParameter.Get(Request);
            var epoch = epochParameter.Get(Request);

            var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), epoch);

            if (source != framingVM.FramingAssistantSource) framingVM.FramingAssistantSource = source;

            var dso = new DeepSkyObject("api request", coordinates, profile.AstrometrySettings.Horizon);

            var success = await framingVM.SetCoordinates(dso);

            if (!success)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Error while loading image");
            }

            var image = ImageService.ResizeBitmap(framingVM.ImageParameter.Image, imageQuery);
            ImageWriter writer = ImageWriter.GetImageWriter(image, imageQuery.Format.Value);

            await Response.Body(writer.Encode(imageQuery.Quality.Value), writer.MimeType).SendAsync();
        }

        // TODO: Get Image endpoint
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
