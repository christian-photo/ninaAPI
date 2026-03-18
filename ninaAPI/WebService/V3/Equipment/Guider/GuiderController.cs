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
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Guider
{
    public class GuiderController : IHttpController
    {
        private readonly IGuiderMediator guider;
        private readonly IApplicationStatusMediator appStatus;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        public GuiderController(IGuiderMediator guider, IApplicationStatusMediator appStatus, ApiProcessMediator processMediator, ISerializerService serializer)
        {
            this.guider = guider;
            this.appStatus = appStatus;
            this.processMediator = processMediator;
            this.serializer = serializer;
        }

        public GuiderInfoResponse GuiderInfo()
        {
            return new GuiderInfoResponse(guider);
        }

        public object StartGuiding(GuiderStartGuidingBody body)
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            // Needs to be a process in case a calibration is needed

            Guid processId = processMediator.AddProcess(
               async (token) => await guider.StartGuiding(body?.ForceCalibration ?? false, appStatus.GetStatus(), token),
               ApiProcessType.GuiderStartGuiding
           );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public async Task<StringResponse> StopGuiding(HttpSession session)
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }

            bool success = await guider.StopGuiding(session.RequestAborted); // TODO: Check why maybe false

            return new StringResponse("Guiding stopped");
        }

        public object Dither()
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

            return (response, statusCode);
        }

        public async Task<StringResponse> ClearCalibration(HttpSession session)
        {
            if (!guider.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Guider);
            }
            else if (!guider.GetInfo().CanClearCalibration)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Guider can not clear calibration");
            }

            bool success = await guider.ClearCalibration(session.RequestAborted); // TODO Check why maybe false

            return new StringResponse("Calibration cleared");
        }

        public object GuidingGraph(HttpRequest request)
        {
            var pagerParameter = PagerParameterSet.Default();
            pagerParameter.Evaluate(request);

            Pager<GuideStep> steps = new Pager<GuideStep>(GuiderWatcher.GuideStepHistory.ToList());

            return steps.GetPage(pagerParameter.PageParameter.Value, pagerParameter.PageSizeParameter.Value);
        }

        public StringResponse SetGuidingHistoryLength(GuidingHistoryLengthBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            GuiderWatcher.GuideStepHistoryLength = body.Length;

            return new StringResponse("History length set");
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => GuiderInfo());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/guiding/start", (HttpRequest request) => StartGuiding(serializer.Deserialize<GuiderStartGuidingBody>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/guiding/stop", async (HttpSession session) => await StopGuiding(session));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/guiding/dither", () => Dither());
            server.Map(HttpVerbs.DELETE.ToString(), prefix + "/guiding/calibration", (HttpSession session) => ClearCalibration(session));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/guiding", (HttpRequest request) => GuidingGraph(request));
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/guiding", (HttpRequest request) => SetGuidingHistoryLength(serializer.Deserialize<GuidingHistoryLengthBody>(request.BodyString)));
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