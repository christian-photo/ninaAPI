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
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using System.Threading.Tasks;
using System.Linq;
using Swan;
using System.Net;
using System.Threading;
using System.Drawing;
using NINA.Core.Model.Equipment;
using NINA.Image.Interfaces;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.Equipment.Model;
using System.Windows.Media.Imaging;
using NINA.PlateSolving.Interfaces;
using NINA.Astrometry;
using ninaAPI.WebService.V3.Service;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using System.IO;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraController : WebApiController
    {
        private readonly ICameraMediator cam;
        private readonly IProfileService profile;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly ITelescopeMediator mount;
        private readonly IFilterWheelMediator filterWheel;

        private readonly ResponseHandler responseHandler;

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
            ResponseHandler responseHandler)
        {
            this.cam = camera;
            this.profile = profile;
            this.imagingMediator = imaging;
            this.imageSaveMediator = imageSave;
            this.statusMediator = status;
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.mount = mount;
            this.filterWheel = filterWheel;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/info")]
        public async Task CameraInfo()
        {
            object response;
            try
            {
                CameraInfoResponse info = CameraInfoResponse.FromCam(cam);
                response = info;
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Put, "/settings/readout")]
        public async Task CameraSetReadout()
        {
            StringResponse response = new StringResponse();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true);

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (cam.GetInfo().IsExposing)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera currently exposing");
                }
                else
                {
                    int readoutModes = cam.GetInfo().ReadoutModes.Count();

                    int mode = modeParameter.Get(HttpContext);

                    if (mode.IsBetween(0, readoutModes))
                    {
                        cam.SetReadoutMode((short)mode);
                        response.Message = "Readout mode updated";
                    }
                    else
                    {
                        throw CommonErrors.ParameterOutOfRange("mode", 0, readoutModes);
                    }
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        // TODO: Use ApiProcess for all these
        private ApiProcess cameraCoolProcess;
        private ApiProcess cameraWarmProcess;

        [Route(HttpVerbs.Post, "/cool")]
        public async Task CameraCool()
        {
            StringResponse response = new StringResponse();

            QueryParameter<double> temperatureParameter = new QueryParameter<double>("temperature", -10, true);
            QueryParameter<double> minutesParameter = new QueryParameter<double>("minutes", profile.ActiveProfile.CameraSettings.CoolingDuration, false, (minutes) => minutes >= 0);
            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (!cam.GetInfo().CanSetTemperature)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera has no temperature control");
                }
                else if (cameraCoolProcess != null && cameraCoolProcess.Status == ApiProcessStatus.Running)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Cooling already running");
                }
                else
                {
                    temperatureParameter.Get(HttpContext);
                    minutesParameter.Get(HttpContext);

                    cameraCoolProcess = new ApiProcess((token) =>
                        cam.CoolCamera(
                            temperatureParameter.Value,
                            TimeSpan.FromMinutes(minutesParameter.Value),
                            statusMediator.GetStatus(),
                            token
                        ));
                    cameraCoolProcess.Start();
                    response.Message = "Cooling started";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/cool/abort")]
        public async Task CameraCoolAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (cameraCoolProcess == null)
                {
                    response.Message = "Cooling not running";
                }
                else if (cameraCoolProcess.Status == ApiProcessStatus.Running || cameraCoolProcess.Status == ApiProcessStatus.Pending)
                {
                    cameraCoolProcess.Stop();
                    response.Message = "Cooling aborted";
                }
                else
                {
                    response.Message = "Cooling not running";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/warm")]
        public async Task CameraWarm()
        {
            StringResponse response = new StringResponse();

            QueryParameter<double> minutesParameter = new QueryParameter<double>("minutes", profile.ActiveProfile.CameraSettings.WarmingDuration, false, (minutes) => minutes >= 0);

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (!cam.GetInfo().CanSetTemperature)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera has no temperature control");
                }
                else if (cameraWarmProcess != null && cameraWarmProcess.Status == ApiProcessStatus.Running)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Cooling or warming already running");
                }
                else
                {
                    minutesParameter.Get(HttpContext);

                    cameraWarmProcess = new ApiProcess((token) =>
                        cam.WarmCamera(
                            TimeSpan.FromMinutes(minutesParameter.Value),
                            statusMediator.GetStatus(),
                            token
                        ));
                    cameraWarmProcess.Start();
                    response.Message = "Warming started";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/warm/abort")]
        public async Task CameraWarmAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (cameraWarmProcess == null)
                {
                    response.Message = "Warming not running";
                }
                else if (cameraWarmProcess.Status == ApiProcessStatus.Running || cameraWarmProcess.Status == ApiProcessStatus.Pending)
                {
                    cameraWarmProcess.Stop();
                    response.Message = "Warming aborted";
                }
                else
                {
                    response.Message = "Warming not running";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/abort-exposure")]
        public async Task AbortExposure()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (!cam.GetInfo().IsExposing)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera not exposing");
                }
                else
                {
                    cam.AbortExposure();
                    response.Message = "Exposure aborted";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Put, "/settings/dew-heater")]
        public async Task CameraDewHeater()
        {
            StringResponse response = new StringResponse();

            QueryParameter<bool> powerParameter = new QueryParameter<bool>("power", false, true);

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (!cam.GetInfo().HasDewHeater)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera has no dew heater");
                }
                else
                {
                    powerParameter.Get(HttpContext);

                    cam.SetDewHeater(powerParameter.Value);
                    response.Message = "Dew heater updated";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Put, "/settings/binning")]
        public async Task CameraSetBinning()
        {
            StringResponse response = new StringResponse();

            SizeQueryParameter binningParameter = new SizeQueryParameter(new Size(1, 1), true, false, "x", "y");

            try
            {
                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else
                {
                    Size binning = binningParameter.Get(HttpContext);
                    BinningMode mode = cam.GetInfo().BinningModes.FirstOrDefault(b => b.X == binning.Width && b.Y == binning.Height, null);
                    if (mode == null)
                    {
                        throw new HttpException(HttpStatusCode.BadRequest, "Invalid binning mode");
                    }
                    else
                    {
                        cam.SetBinning(mode.X, mode.Y);
                        response.Message = "Binning set";
                    }
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        private volatile bool isCaptureBayered = false;
        private volatile int bitDepth = 16;
        private double pixelSize;
        private volatile PlateSolveResult plateSolveResult;
        private ApiProcess cameraCaptureProcess;

        private object captureLock = new object();


        [Route(HttpVerbs.Post, "/capture/start")]
        public async Task CameraCapture()
        {
            StringResponse response = new StringResponse();
            CameraInfo info = cam.GetInfo();

            IPlateSolveSettings settings = profile.ActiveProfile.PlateSolveSettings;

            QueryParameter<double> durationParameter = new QueryParameter<double>("duration", settings.ExposureTime, false, (exp) => exp > 0);
            QueryParameter<int> gainParameter = new QueryParameter<int>("gain", settings.Gain == -1 ? info.Gain : settings.Gain, false, (gain) => gain >= cam.GetInfo().GainMin && gain <= cam.GetInfo().GainMax);
            QueryParameter<bool> saveParameter = new QueryParameter<bool>("save", false, false);
            QueryParameter<double> roiParameter = new QueryParameter<double>("roi", 1.0, false, (roi) => roi > 0 && roi <= 1);

            try
            {
                if (!info.Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (info.IsExposing)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera currently exposing");
                }
                else if (roiParameter.WasProvided && !cam.GetInfo().CanSubSample)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera does not support sub-sampling");
                }
                else if (cameraCaptureProcess != null && cameraCaptureProcess.Status == ApiProcessStatus.Running)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Capture already running");
                }
                else
                {
                    durationParameter.Get(HttpContext);
                    gainParameter.Get(HttpContext);
                    saveParameter.Get(HttpContext);
                    roiParameter.Get(HttpContext);

                    cameraCaptureProcess = new ApiProcess(async (token) =>
                    {
                        CaptureSequence sequence = new CaptureSequence(
                            durationParameter.Value,
                            CaptureSequence.ImageTypes.SNAPSHOT,
                            filterWheel.GetInfo().SelectedFilter,
                            new BinningMode(info.BinX, info.BinY),
                            1);

                        sequence.Gain = gainParameter.Value;

                        if (roiParameter.WasProvided)
                        {
                            var centerX = info.XSize / 2d;
                            var centerY = info.YSize / 2d;
                            var subWidth = info.XSize * roiParameter.Value;
                            var subHeight = info.YSize * roiParameter.Value;
                            var startX = centerX - subWidth / 2d;
                            var startY = centerY - subHeight / 2d;
                            var rect = new ObservableRectangle(startX, startY, subWidth, subHeight);
                            sequence.EnableSubSample = true;
                            sequence.SubSambleRectangle = rect;
                        }

                        PrepareImageParameters parameters = new PrepareImageParameters(autoStretch: true);
                        IExposureData exposure = await imagingMediator.CaptureImage(sequence, token, statusMediator.GetStatus());
                        IRenderedImage renderedImage = await imagingMediator.PrepareImage(exposure, parameters, token);

                        lock (captureLock)
                        {
                            bitDepth = renderedImage.RawImageData.Properties.BitDepth;
                            pixelSize = renderedImage.RawImageData.MetaData.Camera.PixelSize;
                            isCaptureBayered = renderedImage.RawImageData.Properties.IsBayered;
                            plateSolveResult = null;
                        }

                        var encoder = BitmapHelper.GetEncoder(renderedImage.Image, -1);
                        using (FileStream fs = new FileStream(FileSystemHelper.GetCapturePngPath(), FileMode.Create))
                        {
                            encoder.Save(fs);
                        }

                        if (saveParameter.Value)
                        {
                            await imageSaveMediator.Enqueue(renderedImage.RawImageData, Task.Run(() => renderedImage), statusMediator.GetStatus(), token);
                        }
                        // TODO: Should we use IMAGE-PREPARED or API-CAPTURE-FINISHED? await WebSocketV2.SendAndAddEvent("API-CAPTURE-FINISHED");
                    });
                    cameraCaptureProcess.Start();

                    response.Message = "Capture started";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/capture/abort")]
        public async Task CameraCaptureAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (cameraCaptureProcess == null)
                {
                    response.Message = "Capture not running";
                }
                else if (cameraCaptureProcess.Status == ApiProcessStatus.Running || cameraCaptureProcess.Status == ApiProcessStatus.Pending)
                {
                    cam.AbortExposure();
                    cameraCaptureProcess.Stop();
                    response.Message = "Capture aborted";
                }
                else
                {
                    response.Message = "Capture not running";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/capture/get-image")]
        public async Task CameraCaptureImage()
        {
            SensorType sensor = SensorType.Monochrome;

            if (profile.ActiveProfile.CameraSettings.BayerPattern != BayerPatternEnum.Auto)
            {
                sensor = (SensorType)profile.ActiveProfile.CameraSettings.BayerPattern;
            }
            else if (cam.GetInfo().Connected)
            {
                sensor = cam.GetInfo().SensorType;
            }

            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.ByProfile(profile.ActiveProfile);
            imageQuery.BayerPattern = new QueryParameter<SensorType>("bayer-pattern", sensor, false);

            imageQuery.Evaluate(HttpContext);

            try
            {
                if (!File.Exists(FileSystemHelper.GetCapturePngPath()))
                {
                    throw new HttpException(HttpStatusCode.Conflict, "No image available");
                }
                else if ((cameraCaptureProcess?.Status ?? ApiProcessStatus.Finished) == ApiProcessStatus.Running)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
                }
                else
                {
                    BitmapEncoder encoder = await ImageService.ProcessAndPrepareImage(FileSystemHelper.GetCapturePngPath(), isCaptureBayered, imageQuery, bitDepth);
                    using (MemoryStream memory = new MemoryStream())
                    {
                        encoder.Save(memory);
                        await responseHandler.SendBytes(HttpContext, memory.ToArray(), encoder.CodecInfo.MimeTypes);
                        return;
                    }
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }
        }

        [Route(HttpVerbs.Get, "/capture/statistics")]
        public async Task CameraCaptureStats()
        {
            QueryParameter<RawConverterEnum> rawConverterParameter = new QueryParameter<RawConverterEnum>("raw-converter", RawConverterEnum.FREEIMAGE, false);
            QueryParameter<StarSensitivityEnum> starSensitivityParameter = new QueryParameter<StarSensitivityEnum>("star-sensitivity", StarSensitivityEnum.Normal, false);
            QueryParameter<NoiseReductionEnum> noiseReductionParameter = new QueryParameter<NoiseReductionEnum>("noise-reduction", NoiseReductionEnum.None, false);

            try
            {
                if (!File.Exists(FileSystemHelper.GetCapturePngPath()))
                {
                    throw new HttpException(HttpStatusCode.Conflict, "No image available");
                }
                else if ((cameraCaptureProcess?.Status ?? ApiProcessStatus.Finished) == ApiProcessStatus.Running)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
                }
                else
                {
                    rawConverterParameter.Get(HttpContext);
                    starSensitivityParameter.Get(HttpContext);
                    noiseReductionParameter.Get(HttpContext);

                    IImageData imageData = await Retry.Do(
                        async () => await imageDataFactory.CreateFromFile(
                            FileSystemHelper.GetCapturePngPath(),
                            bitDepth,
                            isCaptureBayered,
                            rawConverterParameter.Value
                        ),
                        TimeSpan.FromMilliseconds(200), 10
                    );
                    var img = await imageData.RenderImage().DetectStars(false, starSensitivityParameter.Value, noiseReductionParameter.Value, HttpContext.CancellationToken);
                    var s = ImageStatistics.Create(img.RawImageData);

                    var stats = new
                    {
                        Stars = img.RawImageData.StarDetectionAnalysis.DetectedStars,
                        HFR = img.RawImageData.StarDetectionAnalysis.HFR,
                        Median = s.Median,
                        MedianAbsoluteDeviation = s.MedianAbsoluteDeviation,
                        Mean = s.Mean,
                        Max = s.Max,
                        Min = s.Min,
                        StDev = s.StDev,
                        PixelSize = pixelSize,
                        BitDepth = img.RawImageData.Properties.BitDepth,
                        Width = img.RawImageData.Properties.Width,
                        Height = img.RawImageData.Properties.Height,
                    };

                    await responseHandler.SendObject(HttpContext, stats);
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }
        }

        [Route(HttpVerbs.Post, "/capture/platesolve")]
        public async Task CameraCaptureSolve([JsonData] PlatesolveConfig config)
        {
            IPlateSolveSettings settings = profile.ActiveProfile.PlateSolveSettings;

            try
            {
                if (!File.Exists(FileSystemHelper.GetCapturePngPath()))
                {
                    throw new HttpException(HttpStatusCode.NotFound, "No image available");
                }
                else if ((cameraCaptureProcess?.Status ?? ApiProcessStatus.Finished) == ApiProcessStatus.Running)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
                }
                else
                {
                    if (plateSolveResult is null)
                    {
                        IImageData imageData = await Retry.Do(
                            async () => await imageDataFactory.CreateFromFile(
                                FileSystemHelper.GetCapturePngPath(),
                                bitDepth,
                                isCaptureBayered,
                                (RawConverterEnum)config.RawConverter
                            ),
                            TimeSpan.FromMilliseconds(200), 10
                        );
                        double focalLength = profile.ActiveProfile.TelescopeSettings.FocalLength;
                        CaptureSolverParameter solverParameter = new CaptureSolverParameter()
                        {
                            Attempts = (int)config.Attempts,
                            Binning = (short)config.Binning,
                            BlindFailoverEnabled = (bool)config.BlindFailoverEnabled,
                            Coordinates = new Coordinates(Angle.ByDegree((double)config.RA), Angle.ByDegree((double)config.Dec), Epoch.J2000),
                            DownSampleFactor = (int)config.DownSampleFactor,
                            FocalLength = focalLength,
                            MaxObjects = (int)config.MaxObjects,
                            Regions = (int)config.Regions,
                            SearchRadius = (double)config.SearchRadius,
                            PixelSize = pixelSize
                        };
                        IImageSolver captureSolver = plateSolverFactory.GetImageSolver(
                            plateSolverFactory.GetPlateSolver(settings),
                            plateSolverFactory.GetBlindSolver(settings)
                        );
                        var result = await captureSolver.Solve(imageData, solverParameter, statusMediator.GetStatus(), HttpContext.CancellationToken);

                        lock (captureLock)
                        {
                            plateSolveResult = result;
                        }
                    }

                    await responseHandler.SendObject(HttpContext, plateSolveResult);
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }
        }
    }
}
