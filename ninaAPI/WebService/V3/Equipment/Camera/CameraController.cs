#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using NINA.Equipment.Equipment.MyCamera;
using System.Threading.Tasks;
using System.Linq;
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
using NINA.Equipment.Interfaces;
using System.ComponentModel.DataAnnotations;
using SimpleW;
using ninaAPI.Utility.Serialization;
using NINA.PlateSolving;
using ninaAPI.WebService.V3.Application.Image;
using SimpleW.Service.BasicAuth;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    [Route($"/v3/api/equipment/{EquipmentConstants.CameraUrlName}")]
    [BasicAuth]
    public class CameraController : Controller
    {
        private readonly ICameraMediator cam;
        private readonly ITelescopeMediator mount;
        private readonly IProfileService profile;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        private readonly CaptureMediator captureMediator;

        public CameraController(
            ICameraMediator camera,
            ITelescopeMediator mount,
            IProfileService profile,
            IApplicationStatusMediator status,
            IImageDataFactory imageDataFactory,
            IPlateSolverFactory plateSolverFactory,
            ApiProcessMediator processMediator,
            ISerializerService serializer,
            CaptureMediator captureMediator)
        {
            this.cam = camera;
            this.mount = mount;
            this.profile = profile;
            this.statusMediator = status;
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.processMediator = processMediator;
            this.serializer = serializer;

            this.captureMediator = captureMediator;
        }

        [Route("GET", "/")]
        public async Task<CameraInfoResponse> CameraInfo()
        {
            CameraInfoResponse info = new CameraInfoResponse(cam);

            return info;
        }

        [Route("POST", "/cool")]
        public async Task<object> CameraCool()
        {
            CoolCameraBody body = serializer.Deserialize<CoolCameraBody>(Request.BodyString);
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

            return (response, statusCode);
        }

        [Route("POST", "/warm")]
        public async Task<object> CameraWarm()
        {
            WarmCameraBody body = serializer.Deserialize<WarmCameraBody>(Request.BodyString);
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

            return (response, statusCode);
        }

        [Route("POST", "/abort-exposure")]
        public async Task<StringResponse> AbortExposure()
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

            return new StringResponse("Exposure aborted");
        }

        [Route("PUT", "/dew-heater")]
        public async Task<StringResponse> CameraDewHeater()
        {
            DewHeaterUpdateBody body = serializer.Deserialize<DewHeaterUpdateBody>(Request.BodyString);
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

            return new StringResponse("Dew heater power set");
        }

        [Route("PUT", "/binning")]
        public async Task<StringResponse> CameraSetBinning()
        {
            BinningMode binning = serializer.Deserialize<BinningMode>(Request.BodyString);
            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (binning == null || !cam.GetInfo().BinningModes.Any(b => b.X == binning.X && b.Y == binning.Y))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid binning mode");
            }

            cam.SetBinning(binning.X, binning.Y);

            return new StringResponse("Binning set");
        }

        [Route("PUT", "/usb-limit")]
        public async Task<StringResponse> CameraSetUsbLimit()
        {
            USBLimitUpdateBody body = serializer.Deserialize<USBLimitUpdateBody>(Request.BodyString);
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

            return new StringResponse("USB limit set");
        }

        [Route("PUT", "/readout")]
        public async Task<StringResponse> CameraSetReadout()
        {
            ReadoutModeUpdateBody body = serializer.Deserialize<ReadoutModeUpdateBody>(Request.BodyString);
            Validator.ValidateObject(body, new ValidationContext(body));

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

            if (body.Persistent)
            {
                profile.ActiveProfile.CameraSettings.ReadoutMode = body.Mode;
            }

            return new StringResponse("Readout mode updated");
        }

        [Route("PUT", "/readout/normal")]
        public async Task<StringResponse> CameraSetReadoutNormal()
        {
            ReadoutModeUpdateBody body = serializer.Deserialize<ReadoutModeUpdateBody>(Request.BodyString);
            Validator.ValidateObject(body, new ValidationContext(body));

            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (body.Mode >= readoutModes)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Mode), 0, readoutModes - 1);
            }

            ((ICamera)cam.GetDevice()).ReadoutModeForNormalImages = body.Mode;

            if (body.Persistent)
            {
                profile.ActiveProfile.CameraSettings.ReadoutModeForNormalImages = body.Mode;
            }

            return new StringResponse("Readout mode updated");
        }

        [Route("PUT", "/readout/snapshot")]
        public async Task<StringResponse> CameraSetReadoutSnapshot()
        {
            ReadoutModeUpdateBody body = serializer.Deserialize<ReadoutModeUpdateBody>(Request.BodyString);
            Validator.ValidateObject(body, new ValidationContext(body));

            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }
            else if (body.Mode >= readoutModes)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Mode), 0, readoutModes - 1);
            }

            ((ICamera)cam.GetDevice()).ReadoutModeForSnapImages = body.Mode;

            if (body.Persistent)
            {
                profile.ActiveProfile.CameraSettings.ReadoutModeForSnapImages = body.Mode;
            }

            return new StringResponse("Readout mode updated");
        }

        [Route("POST", "/capture")]
        public async Task<object> CameraCapture()
        {
            CaptureConfig config = serializer.Deserialize<CaptureConfig>(Request.BodyString);
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

            return (response, statusCode);
        }

        [Route("GET", "/capture/:id")]
        public async Task CameraCaptureImage(Guid id)
        {
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile.ActiveProfile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", FindBayer(profile.ActiveProfile, cam), false);

            imageQuery.Evaluate(Request);

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

            await Response.Body(writer.Encode(imageQuery.Quality.Value), writer.MimeType).SendAsync();
        }

        [Route("GET", "/capture/:id/analysis")]
        public async Task<object> CameraCaptureStats(Guid id)
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

            rawConverterParameter.Get(Request);
            starSensitivityParameter.Get(Request);
            noiseReductionParameter.Get(Request);

            var stats = await capture.Analyze(imageDataFactory, starSensitivityParameter.Value, noiseReductionParameter.Value, rawConverterParameter.Value, Session.RequestAborted);

            return stats;
        }

        [Route("GET", "/capture/:id/solve")]
        public async Task<PlateSolveResult> CameraCaptureSolve(Guid id)
        {
            PlatesolveConfig config = serializer.Deserialize<PlatesolveConfig>(Request.BodyString);
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

            var result = await capture.GetPlateSolve(imageDataFactory, plateSolverFactory, config, Session.RequestAborted);

            return result;
        }

        /// <summary>
        /// Removes a capture and cleans everything up. This is unique to capture since it stores data on the disk
        /// and the user might want to clean it up without having to exit NINA
        /// </summary>
        /// <param name="id">The id of the capture that will be removed</param>
        /// <returns></returns>
        [Route("DELETE", "/capture/:id")]
        public async Task<StringResponse> CameraRemoveCapture(Guid id)
        {
            var capture = captureMediator.GetCapture(id) ?? throw new HttpException(HttpStatusCode.NotFound, "Capture not found");

            capture.Stop();
            captureMediator.RemoveCapture(id);

            return new StringResponse("Capture removed");
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

        /// <summary>
        /// If true, the readout mode should be written in the profile settings such that it is still active after a reconnect
        /// </summary>
        public bool Persistent { get; set; }
    }
}
