#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
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

        [Route(HttpVerbs.Get, "/")]
        public async Task GuiderInfo()
        {
            await responseHandler.SendObject(HttpContext, new GuiderInfoResponse(guider));
        }

        [Route(HttpVerbs.Post, "/guiding/start")]
        public async Task StartGuiding()
        {
            QueryParameter<bool> forceCalibration = new QueryParameter<bool>("forceCalibration", false, false);

            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            bool force = forceCalibration.Get(HttpContext);

            Guid processId = processMediator.AddProcess(
               async (token) => await guider.StartGuiding(force, appStatus.GetStatus(), token),
               ApiProcessType.GuiderStartGuiding
           );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/guiding/stop")]
        public async Task StopGuiding()
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            await guider.StopGuiding(HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, new StringResponse("Guiding stopped"));
        }

        [Route(HttpVerbs.Post, "/guiding/dither")]
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

        [Route(HttpVerbs.Delete, "/calibration")]
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

            await guider.ClearCalibration(HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, new StringResponse("Calibration cleared"));
        }

        [Route(HttpVerbs.Get, "/guiding/graph")]
        public async Task GuidingGraph()
        {
            var handlerField = guider.GetType().GetField("handler",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            GuiderVM gvm = (GuiderVM)handlerField.GetValue(guider);

            await responseHandler.SendObject(HttpContext, gvm.GuideStepsHistory);
        }
    }
}