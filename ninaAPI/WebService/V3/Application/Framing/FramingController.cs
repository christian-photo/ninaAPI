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
using Accord;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Application.Framing
{
    public class FramingController : WebApiController
    {
        private readonly IFramingAssistantVM framingVM;
        private readonly ICameraMediator camera;
        private readonly IProfileService profileService;
        private readonly ApiProcessMediator processMediator;
        private readonly ResponseHandler responseHandler;

        public FramingController(IFramingAssistantVM framingVm, ICameraMediator camera, IProfileService profileService, ApiProcessMediator processMediator, ResponseHandler responseHandler)
        {
            this.framingVM = framingVm;
            this.camera = camera;
            this.profileService = profileService;
            this.processMediator = processMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task FramingInfo()
        {
            await responseHandler.SendObject(HttpContext, new FramingInfoContainer(framingVM));
        }

        [Route(HttpVerbs.Patch, "/")]
        public async Task FramingUpdate([JsonData] FramingUpdate config)
        {
            Validator.ValidateObject(config, new ValidationContext(config)); // is there a better way to do this?

            if (config.BoundHeight != null)
            {
                framingVM.BoundHeight = config.BoundHeight.Value;
            }
            if (config.BoundWidth != null)
            {
                framingVM.BoundWidth = config.BoundWidth.Value;
            }
            if (config.CameraHeight != null)
            {
                framingVM.CameraHeight = config.CameraHeight.Value;
            }
            if (config.CameraWidth != null)
            {
                framingVM.CameraWidth = config.CameraWidth.Value;
            }
            if (config.CameraPixelSize != null)
            {
                framingVM.CameraPixelSize = config.CameraPixelSize.Value;
            }
            if (config.Coordinates != null)
            {
                await framingVM.SetCoordinates(new DeepSkyObject(framingVM.DSO.Name, config.Coordinates.ToCoordinates(), profileService.ActiveProfile.AstrometrySettings.Horizon));
            }
            if (!string.IsNullOrEmpty(config.DSOName))
            {
                framingVM.DSO.Name = config.DSOName;
            }
            if (config.FieldOfView != null)
            {
                framingVM.FieldOfView = config.FieldOfView.Value;
            }
            if (config.FocalLength != null)
            {
                framingVM.FocalLength = config.FocalLength.Value;
            }
            if (config.HorizontalPanels != null)
            {
                framingVM.HorizontalPanels = config.HorizontalPanels.Value;
            }
            if (config.VerticalPanels != null)
            {
                framingVM.VerticalPanels = config.VerticalPanels.Value;
            }
            if (config.FramingSource != null)
            {
                framingVM.FramingAssistantSource = config.FramingSource.Value;
            }

            await responseHandler.SendObject(HttpContext, new FramingInfoContainer(framingVM));
        }

        // I dont copy the slew endopint because you can use the mount slew as well

        [Route(HttpVerbs.Post, "/solve-rotation")]
        public async Task FramingSolveRotation()
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

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }
    }
}