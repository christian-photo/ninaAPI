#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Guider
{
    public class GuiderController : WebApiController
    {
        private readonly IGuiderMediator guider;
        private readonly IApplicationStatusMediator appStatus;
        private readonly ApiProcessMediator processMediator;
        private readonly ResponseHandler responseHandler;

        public GuiderController(IGuiderMediator guider, IApplicationStatusMediator appStatus, ApiProcessMediator processMediator, ResponseHandler responseHandler)
        {
            this.guider = guider;
            this.appStatus = appStatus;
            this.processMediator = processMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, $"/{EquipmentConstants.GuiderUrlName}")]
        public async Task GuiderInfo()
        {
            await responseHandler.SendObject(HttpContext, new GuiderInfoResponse(guider));
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.GuiderUrlName}/guiding/start")]
        public async Task StartGuiding([JsonData] GuiderStartGuidingBody body)
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            Guid processId = processMediator.AddProcess(
               async (token) => await guider.StartGuiding(body?.ForceCalibration ?? false, appStatus.GetStatus(), token),
               ApiProcessType.GuiderStartGuiding
           );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.GuiderUrlName}/guiding/stop")]
        public async Task StopGuiding()
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            bool success = await guider.StopGuiding(HttpContext.CancellationToken); // TODO: Check why maybe false

            await responseHandler.SendObject(HttpContext, new StringResponse("Guiding stopped"));
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.GuiderUrlName}/guiding/dither")]
        public async Task Dither()
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            Guid processId = processMediator.AddProcess(
               async (token) => await guider.Dither(token),
               ApiProcessType.GuiderDither
           );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Delete, $"/{EquipmentConstants.GuiderUrlName}/guiding/calibration")]
        public async Task ClearCalibration()
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }
            else if (!guider.GetInfo().CanClearCalibration)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Guider can not clear calibration");
            }

            bool success = await guider.ClearCalibration(HttpContext.CancellationToken); // TODO Check why maybe false

            await responseHandler.SendObject(HttpContext, new StringResponse("Calibration cleared"));
        }

        [Route(HttpVerbs.Get, $"/{EquipmentConstants.GuiderUrlName}/guiding")]
        public async Task GuidingGraph()
        {
            var pagerParameter = PagerParameterSet.Default();
            pagerParameter.Evaluate(HttpContext);

            Pager<GuideStep> steps = new Pager<GuideStep>(GuiderWatcher.GuideStepHistory.ToList());

            await responseHandler.SendObject(HttpContext, steps.GetPage(pagerParameter.PageParameter.Value, pagerParameter.PageSizeParameter.Value));
        }

        [Route(HttpVerbs.Patch, $"/{EquipmentConstants.GuiderUrlName}/guiding")]
        public async Task SetGuidingHistoryLength([JsonData] GuidingHistoryLengthBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            GuiderWatcher.GuideStepHistoryLength = body.Length;

            await responseHandler.SendObject(HttpContext, new StringResponse("History length set"));
        }
    }

    public class GuiderStartGuidingBody
    {
        public bool ForceCalibration { get; set; }
    }

    public class GuidingHistoryLengthBody
    {
        [Required]
        public int Length { get; set; }
    }
}