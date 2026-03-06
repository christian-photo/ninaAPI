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
using NINA.Equipment.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraController : WebApiController
    {
        private readonly ICameraMediator cam;
        private readonly ITelescopeMediator mount;
        private readonly IProfileService profile;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly ApiProcessMediator processMediator;

        private readonly ResponseHandler responseHandler;
        private readonly CaptureMediator captureMediator;

        public CameraController(
            ICameraMediator camera,
            ITelescopeMediator mount,
            IProfileService profile,
            IImagingMediator imaging,
            IImageSaveMediator imageSave,
            IApplicationStatusMediator status,
            IImageDataFactory imageDataFactory,
            IPlateSolverFactory plateSolverFactory,
            IFilterWheelMediator filterWheel,
            ResponseHandler responseHandler,
            ApiProcessMediator processMediator)
        {
            this.cam = camera;
            this.mount = mount;
            this.profile = profile;
            this.statusMediator = status;
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.responseHandler = responseHandler;
            this.processMediator = processMediator;

            this.captureMediator = new CaptureMediator(camera, filterWheel, profile, imaging, imageSave, status, processMediator);
        }

        [Route(HttpVerbs.Get, $"/{EquipmentConstants.CameraUrlName}")]
        public async Task CameraInfo()
        {
            CameraInfoResponse info = new CameraInfoResponse(cam);

            await responseHandler.SendObject(HttpContext, info);
        }

        // Documented
        [Route(HttpVerbs.Post, $"/{EquipmentConstants.CameraUrlName}/cool")]
        public async Task CameraCool([JsonData] CoolCameraBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            var duration = body.Duration ?? profile.ActiveProfile.CameraSettings.CoolingDuration;

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().CanSetTemperature)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera has no temperature control");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await cam.CoolCamera(body.Temperature, TimeSpan.FromMinutes(duration), statusMediator.GetStatus(), token),
                ApiProcessType.CameraCool
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        // Documented
        [Route(HttpVerbs.Post, $"/{EquipmentConstants.CameraUrlName}/warm")]
        public async Task CameraWarm([JsonData] WarmCameraBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            var duration = body.Duration ?? profile.ActiveProfile.CameraSettings.WarmingDuration;

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().CanSetTemperature)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera has no temperature control");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await cam.WarmCamera(TimeSpan.FromMinutes(duration), statusMediator.GetStatus(), token),
                ApiProcessType.CameraWarm
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        // Documented
        [Route(HttpVerbs.Post, $"/{EquipmentConstants.CameraUrlName}/abort-exposure")]
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

        // Documented
        [Route(HttpVerbs.Put, $"/{EquipmentConstants.CameraUrlName}/settings/dew-heater")]
        public async Task CameraDewHeater([JsonData] DewHeaterUpdateBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (!cam.GetInfo().HasDewHeater)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Camera has no dew heater");
            }

            cam.SetDewHeater(body.Power);

            await responseHandler.SendObject(HttpContext, new StringResponse("Dew heater power set"));
        }

        // Documented
        [Route(HttpVerbs.Put, $"/{EquipmentConstants.CameraUrlName}/settings/binning")]
        public async Task CameraSetBinning([JsonData] BinningMode binning)
        {
            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (binning == null || !cam.GetInfo().BinningModes.Any(b => b.X == binning.X && b.Y == binning.Y))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid binning mode");
            }

            cam.SetBinning(binning.X, binning.Y);

            await responseHandler.SendObject(HttpContext, new StringResponse("Binning set"));
        }

        // Documented
        [Route(HttpVerbs.Put, $"/{EquipmentConstants.CameraUrlName}/settings/usb-limit")]
        public async Task CameraSetBinning([JsonData] USBLimitUpdateBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));
            var info = cam.GetInfo();

            if (!info.Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            if (body.Limit < info.USBLimitMin || body.Limit > info.USBLimitMax)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Limit), info.USBLimitMin, info.USBLimitMax);
            }

            cam.SetUSBLimit(body.Limit);

            await responseHandler.SendObject(HttpContext, new StringResponse("USB limit set"));
        }

        // Documented
        [Route(HttpVerbs.Put, $"/{EquipmentConstants.CameraUrlName}/settings/readout")]
        public async Task CameraSetReadout([JsonData] ReadoutModeUpdateBody body)
        {
            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (body.Mode >= readoutModes)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Mode), 0, readoutModes - 1);
            }

            cam.SetReadoutMode(body.Mode);

            await responseHandler.SendObject(HttpContext, new StringResponse("Readout mode updated"));
        }

        // Documented
        [Route(HttpVerbs.Put, $"/{EquipmentConstants.CameraUrlName}/settings/readout/image")]
        public async Task CameraSetReadoutNormal()
        {
            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true, (mode) => mode.IsBetween(0, readoutModes - 1));

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }

            int mode = modeParameter.Get(HttpContext);

            ((ICamera)cam.GetDevice()).ReadoutModeForNormalImages = (short)mode;

            await responseHandler.SendObject(HttpContext, new StringResponse("Readout mode updated"));
        }

        // Documented
        [Route(HttpVerbs.Put, $"/{EquipmentConstants.CameraUrlName}/settings/readout/snapshot")]
        public async Task CameraSetReadoutSnapshot()
        {
            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true, (mode) => mode.IsBetween(0, readoutModes - 1));

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }

            int mode = modeParameter.Get(HttpContext);

            ((ICamera)cam.GetDevice()).ReadoutModeForSnapImages = (short)mode;

            await responseHandler.SendObject(HttpContext, new StringResponse("Readout mode updated"));
        }


        // Documented
        [Route(HttpVerbs.Post, $"/{EquipmentConstants.CameraUrlName}/capture")]
        public async Task CameraCapture([JsonData] CaptureConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

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

        // Documented
        [Route(HttpVerbs.Get, $"/{EquipmentConstants.CameraUrlName}/capture/{{id}}")]
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
            ImageWriter writer = await ImageService.ProcessAndPrepareImage(capture.GetCapturePath(), capture.IsCaptureBayered, imageQuery, capture.BitDepth);

            await responseHandler.SendBytes(HttpContext, writer.Encode(imageQuery.Quality.Value), writer.MimeType);
        }

        // Documented
        [Route(HttpVerbs.Get, $"/{EquipmentConstants.CameraUrlName}/capture/{{id}}/analysis")]
        public async Task CameraCaptureStats(Guid id)
        {
            QueryParameter<RawConverterEnum> rawConverterParameter = new QueryParameter<RawConverterEnum>("raw-converter", profile.ActiveProfile.CameraSettings.RawConverter, false);
            QueryParameter<StarSensitivityEnum> starSensitivityParameter = new QueryParameter<StarSensitivityEnum>("star-sensitivity", profile.ActiveProfile.ImageSettings.StarSensitivity, false);
            QueryParameter<NoiseReductionEnum> noiseReductionParameter = new QueryParameter<NoiseReductionEnum>("noise-reduction", profile.ActiveProfile.ImageSettings.NoiseReduction, false);

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

        // Documented
        [Route(HttpVerbs.Post, $"/{EquipmentConstants.CameraUrlName}/capture/{{id}}/solve")]
        public async Task CameraCaptureSolve(Guid id, [JsonData] PlatesolveConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

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

            config.UpdateDefaults(profile.ActiveProfile, mount, cam);

            var result = await capture.GetPlateSolve(imageDataFactory, plateSolverFactory, config, HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, result);
        }

        // Documented
        /// <summary>
        /// Removes a capture and cleans everything up. This is unique to capture since it stores data on the disk
        /// and the user might want to clean it up without having to exit NINA
        /// </summary>
        /// <param name="id">The id of the capture that will be removed</param>
        /// <returns></returns>
        [Route(HttpVerbs.Delete, $"/{EquipmentConstants.CameraUrlName}/capture/{{id}}")]
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

    public class DewHeaterUpdateBody
    {
        [Required]
        public bool Power { get; set; }
    }

    public class USBLimitUpdateBody
    {
        [Required]
        public int Limit { get; set; }
    }

    public class CoolCameraBody
    {
        [Required]
        public double Temperature { get; set; }

        [Range(0, double.MaxValue)]
        public double? Duration { get; set; }
    }

    public class WarmCameraBody
    {
        [Range(0, double.MaxValue)]
        public double? Duration { get; set; }
    }

    public class ReadoutModeUpdateBody
    {
        [Required]
        [Range(0, short.MaxValue)]
        public short Mode { get; set; }
    }
}
