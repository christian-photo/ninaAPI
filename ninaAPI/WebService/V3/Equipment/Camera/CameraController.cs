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
using ninaAPI.WebService.V3.Model;
using NINA.Equipment.Interfaces;
using System.ComponentModel.DataAnnotations;
using SimpleW;
using ninaAPI.WebService.Interfaces;
using ninaAPI.Utility.Serialization;
using NINA.PlateSolving;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraController : IHttpController
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
            IImagingMediator imaging,
            IImageSaveMediator imageSave,
            IApplicationStatusMediator status,
            IImageDataFactory imageDataFactory,
            IPlateSolverFactory plateSolverFactory,
            IFilterWheelMediator filterWheel,
            ApiProcessMediator processMediator,
            ISerializerService serializer)
        {
            this.cam = camera;
            this.mount = mount;
            this.profile = profile;
            this.statusMediator = status;
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.processMediator = processMediator;
            this.serializer = serializer;

            this.captureMediator = new CaptureMediator(camera, filterWheel, profile, imaging, imageSave, status, processMediator);
        }

        public async Task<CameraInfoResponse> CameraInfo()
        {
            CameraInfoResponse info = new CameraInfoResponse(cam);

            return info;
        }

        public async Task<object> CameraCool(CoolCameraBody body)
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

            return (response, statusCode);
        }

        public async Task<object> CameraWarm(WarmCameraBody body)
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

            return (response, statusCode);
        }

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

        public async Task<StringResponse> CameraDewHeater(DewHeaterUpdateBody body)
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

            return new StringResponse("Dew heater power set");
        }

        public async Task<StringResponse> CameraSetBinning(BinningMode binning)
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

            return new StringResponse("Binning set");
        }

        public async Task<StringResponse> CameraSetBinning(USBLimitUpdateBody body)
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

            return new StringResponse("USB limit set");
        }

        public async Task<StringResponse> CameraSetReadout(ReadoutModeUpdateBody body)
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

            return new StringResponse("Readout mode updated");
        }

        public async Task<StringResponse> CameraSetReadoutNormal(HttpRequest request)
        {
            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true, (mode) => mode.IsBetween(0, readoutModes - 1));

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }

            int mode = modeParameter.Get(request);

            ((ICamera)cam.GetDevice()).ReadoutModeForNormalImages = (short)mode;

            return new StringResponse("Readout mode updated");
        }

        public async Task<StringResponse> CameraSetReadoutSnapshot(HttpRequest request)
        {
            int readoutModes = cam.GetInfo().ReadoutModes.Count();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true, (mode) => mode.IsBetween(0, readoutModes - 1));

            if (!cam.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Camera);
            }

            int mode = modeParameter.Get(request);

            ((ICamera)cam.GetDevice()).ReadoutModeForSnapImages = (short)mode;

            return new StringResponse("Readout mode updated");
        }


        public async Task<object> CameraCapture(CaptureConfig config)
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

            return (response, statusCode);
        }

        public async Task CameraCaptureImage(HttpSession session, Guid id)
        {
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile.ActiveProfile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", FindBayer(profile.ActiveProfile, cam), false);

            imageQuery.Evaluate(session.Request);

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

            await session.Response.Body(writer.Encode(imageQuery.Quality.Value), writer.MimeType).SendAsync();
        }

        public async Task<object> CameraCaptureStats(HttpSession session, Guid id)
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

            rawConverterParameter.Get(session.Request);
            starSensitivityParameter.Get(session.Request);
            noiseReductionParameter.Get(session.Request);

            var stats = await capture.Analyze(imageDataFactory, starSensitivityParameter.Value, noiseReductionParameter.Value, rawConverterParameter.Value, session.RequestAborted);

            return stats;
        }

        public async Task<PlateSolveResult> CameraCaptureSolve(HttpSession session, Guid id, PlatesolveConfig config)
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

            var result = await capture.GetPlateSolve(imageDataFactory, plateSolverFactory, config, session.RequestAborted);

            return result;
        }

        /// <summary>
        /// Removes a capture and cleans everything up. This is unique to capture since it stores data on the disk
        /// and the user might want to clean it up without having to exit NINA
        /// </summary>
        /// <param name="id">The id of the capture that will be removed</param>
        /// <returns></returns>
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

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, async () => await CameraInfo());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/cool", async (HttpRequest request) => await CameraCool(serializer.Deserialize<CoolCameraBody>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/warm", async (HttpRequest request) => await CameraWarm(serializer.Deserialize<WarmCameraBody>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/abort-exposure", async () => await AbortExposure());
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/dew-heater", async (HttpRequest request) => await CameraDewHeater(serializer.Deserialize<DewHeaterUpdateBody>(request.BodyString)));
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/binning", async (HttpRequest request) => await CameraSetBinning(serializer.Deserialize<BinningMode>(request.BodyString)));
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/usb-limit", async (HttpRequest request) => await CameraSetBinning(serializer.Deserialize<USBLimitUpdateBody>(request.BodyString)));
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/readout", async (HttpRequest request) => await CameraSetReadout(serializer.Deserialize<ReadoutModeUpdateBody>(request.BodyString)));
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/readout/image", async (HttpRequest request) => await CameraSetReadoutNormal(request));
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/readout/snapshot", async (HttpRequest request) => await CameraSetReadoutSnapshot(request));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/capture", async (HttpRequest request) => await CameraCapture(serializer.Deserialize<CaptureConfig>(request.BodyString)));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/capture/:id", async (HttpSession session, Guid id) => await CameraCaptureImage(session, id));
            server.Map(HttpVerbs.DELETE.ToString(), prefix + "/capture/:id", async (HttpSession session, Guid id) => await CameraRemoveCapture(id));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/capture/:id/analysis", async (HttpSession session, Guid id) => await CameraCaptureStats(session, id));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/capture/:id/solve", async (HttpSession session, Guid id) => await CameraCaptureSolve(session, id, serializer.Deserialize<PlatesolveConfig>(session.Request.BodyString)));
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
