#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using NINA.Equipment.Equipment.MyCamera;
using System.Threading.Tasks;
using System.Linq;
using Swan;
using System.Net;
using NINA.Core.Model.Equipment;
using NINA.Image.Interfaces;
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.PlateSolving.Interfaces;
using ninaAPI.WebService.V3.Service;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using System.IO;
using ninaAPI.WebService.V3.Model;
using System.Windows.Media.Imaging;
using NINA.Core.Utility;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraController : WebApiController
    {
        private readonly ICameraMediator cam;
        private readonly IProfileService profile;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly ApiProcessMediator processMediator;

        private readonly ResponseHandler responseHandler;
        private readonly CaptureMediator captureMediator;

        public CameraController(
            ICameraMediator camera,
            IProfileService profile,
            IImagingMediator imaging,
            IImageSaveMediator imageSave,
            IApplicationStatusMediator status,
            IImageDataFactory imageDataFactory,
            IPlateSolverFactory plateSolverFactory,
            ITelescopeMediator mount,
            IFilterWheelMediator filterWheel,
            ResponseHandler responseHandler,
            ApiProcessMediator processMediator)
        {
            this.cam = camera;
            this.profile = profile;
            this.statusMediator = status;
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.responseHandler = responseHandler;
            this.processMediator = processMediator;

            this.captureMediator = new CaptureMediator(camera, filterWheel, profile, imaging, imageSave, status, processMediator);
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task CameraInfo()
        {
            CameraInfoResponse info = new CameraInfoResponse(cam);

            await responseHandler.SendObject(HttpContext, info);
        }

        [Route(HttpVerbs.Put, "/settings/readout")]
        public async Task CameraSetReadout()
        {
            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true, (mode) => mode.IsBetween(0, readoutModes));

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }

            int mode = modeParameter.Get(HttpContext);

            cam.SetReadoutMode((short)mode);

            await responseHandler.SendObject(HttpContext, new StringResponse("Readout mode updated"));
        }


        [Route(HttpVerbs.Post, "/cool")]
        public async Task CameraCool()
        {
            QueryParameter<double> temperatureParameter = new QueryParameter<double>("temperature", double.NaN, true);
            QueryParameter<double> minutesParameter = new QueryParameter<double>("minutes", profile.ActiveProfile.CameraSettings.CoolingDuration, false, (minutes) => minutes >= 0);

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().CanSetTemperature)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera has no temperature control");
            }

            temperatureParameter.Get(HttpContext);
            minutesParameter.Get(HttpContext);

            Guid processId = processMediator.AddProcess(
                async (token) => await cam.CoolCamera(temperatureParameter.Value, TimeSpan.FromMinutes(minutesParameter.Value), statusMediator.GetStatus(), token),
                ApiProcessType.CameraCool
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/warm")]
        public async Task CameraWarm()
        {
            QueryParameter<double> minutesParameter = new QueryParameter<double>("minutes", profile.ActiveProfile.CameraSettings.WarmingDuration, false, (minutes) => minutes >= 0);

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().CanSetTemperature)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera has no temperature control");
            }
            minutesParameter.Get(HttpContext);

            Guid processId = processMediator.AddProcess(
                async (token) => await cam.WarmCamera(TimeSpan.FromMinutes(minutesParameter.Value), statusMediator.GetStatus(), token),
                ApiProcessType.CameraWarm
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/abort-exposure")]
        public async Task AbortExposure()
        {
            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().IsExposing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera not exposing");
            }

            cam.AbortExposure();

            await responseHandler.SendObject(HttpContext, new StringResponse("Exposure aborted"));
        }

        [Route(HttpVerbs.Put, "/settings/dew-heater")]
        public async Task CameraDewHeater()
        {
            QueryParameter<bool> powerParameter = new QueryParameter<bool>("power", false, true);

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().HasDewHeater)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera has no dew heater");
            }

            powerParameter.Get(HttpContext);

            cam.SetDewHeater(powerParameter.Value);

            await responseHandler.SendObject(HttpContext, new StringResponse("Dew heater updated"));
        }

        [Route(HttpVerbs.Put, "/settings/binning")]
        public async Task CameraSetBinning([JsonData] BinningMode binning)
        {
            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().BinningModes.Any(b => b.X == binning.X && b.Y == binning.Y))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid binning mode");
            }

            cam.SetBinning(binning.X, binning.Y);

            await responseHandler.SendObject(HttpContext, new StringResponse("Binning set"));
        }


        [Route(HttpVerbs.Post, "/capture")]
        public async Task CameraCapture([JsonData] CaptureConfig config)
        {
            CameraInfo info = cam.GetInfo();
            IPlateSolveSettings settings = profile.ActiveProfile.PlateSolveSettings;

            if (!info.Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (info.IsExposing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera currently exposing");
            }
            else if (config.ROI < 1 && !cam.GetInfo().CanSubSample)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera does not support sub-sampling");
            }
            else if (config.Gain < cam.GetInfo().GainMin || config.Gain > cam.GetInfo().GainMax)
            {
                throw new HttpException(HttpStatusCode.Conflict, $"Gain is outside of range: {cam.GetInfo().GainMin} - {cam.GetInfo().GainMax}");
            }

            config.UpdateDefaults(settings, cam.GetInfo());

            var capture = captureMediator.AddCapture();
            var result = capture.Start(config);

            object response;
            int statusCode = 200;

            if (result == ApiProcessStartResult.Conflict)
            {
                response = ResponseFactory.CreateProcessConflictsResponse(processMediator, processMediator.GetProcess(capture.CaptureId, out var process) ? process : null);
                statusCode = 409;
            }
            else
            {
                response = new
                {
                    CaptureId = capture.CaptureId,
                    FinalizeCaptureProcessId = capture.CaptureFinalizeProcessId,
                };
            }

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Get, "/capture/{id}")]
        public async Task CameraCaptureImage(Guid id)
        {
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile.ActiveProfile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", FindBayer(profile.ActiveProfile, cam), false);

            imageQuery.Evaluate(HttpContext);

            var capture = captureMediator.GetCapture(id);
            if (capture == null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Capture not found");
            }
            else if (capture.GetCaptureFinalizeProcess().Status != ApiProcessStatus.Finished)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
            }
            else if (!File.Exists(capture.GetCapturePath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "No image available");
            }

            BitmapEncoder encoder = await ImageService.ProcessAndPrepareImage(capture.GetCapturePath(), capture.IsCaptureBayered, imageQuery, capture.BitDepth);

            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                await responseHandler.SendBytes(HttpContext, memory.ToArray(), encoder.CodecInfo.MimeTypes);
            }
        }

        [Route(HttpVerbs.Get, "/capture/{id}/analysis")]
        public async Task CameraCaptureStats(Guid id)
        {
            QueryParameter<RawConverterEnum> rawConverterParameter = new QueryParameter<RawConverterEnum>("raw-converter", RawConverterEnum.FREEIMAGE, false);
            QueryParameter<StarSensitivityEnum> starSensitivityParameter = new QueryParameter<StarSensitivityEnum>("star-sensitivity", StarSensitivityEnum.Normal, false);
            QueryParameter<NoiseReductionEnum> noiseReductionParameter = new QueryParameter<NoiseReductionEnum>("noise-reduction", NoiseReductionEnum.None, false);

            var capture = captureMediator.GetCapture(id);
            if (capture == null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Capture not found");
            }
            else if (capture.GetCaptureFinalizeProcess().Status != ApiProcessStatus.Finished)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
            }
            else if (!File.Exists(capture.GetCapturePath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image not available");
            }

            rawConverterParameter.Get(HttpContext);
            starSensitivityParameter.Get(HttpContext);
            noiseReductionParameter.Get(HttpContext);

            var stats = await capture.Analyze(imageDataFactory, starSensitivityParameter.Value, noiseReductionParameter.Value, rawConverterParameter.Value, HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, stats);
        }

        [Route(HttpVerbs.Post, "/capture/{id}/platesolve")]
        public async Task CameraCaptureSolve(Guid id, [JsonData] PlatesolveConfig config)
        {
            var capture = captureMediator.GetCapture(id);
            if (capture == null)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Capture not found");
            }
            else if (capture.GetCaptureFinalizeProcess().Status != ApiProcessStatus.Finished)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
            }
            else if (!File.Exists(capture.GetCapturePath()))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Image not available");
            }

            var result = await capture.GetPlateSolve(imageDataFactory, plateSolverFactory, config, HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, result);
        }

        /// <summary>
        /// Removes a capture and cleans everything up. This is unique to capture since it stores data on the disk
        /// and the user might want to clean it up without having to exit NINA
        /// </summary>
        /// <param name="id">The id of the capture that will be removed</param>
        /// <returns></returns>
        [Route(HttpVerbs.Delete, "/capture/{id}/remove")]
        public async Task CameraRemoveCapture(Guid id)
        {
            var capture = captureMediator.GetCapture(id) ?? throw new HttpException(HttpStatusCode.NotFound, "Capture not found");

            capture.Stop();
            captureMediator.RemoveCapture(id);

            await responseHandler.SendObject(HttpContext, new StringResponse("Capture removed"));
        }

        public static SensorType FindBayer(IProfile profile, ICameraMediator cameraMediator)
        {
            SensorType sensor = SensorType.Monochrome;

            if (profile.CameraSettings.BayerPattern != BayerPatternEnum.Auto)
            {
                sensor = (SensorType)profile.CameraSettings.BayerPattern;
            }
            else if (cameraMediator.GetInfo().Connected)
            {
                sensor = cameraMediator.GetInfo().SensorType;
            }

            return sensor;
        }
    }
}
