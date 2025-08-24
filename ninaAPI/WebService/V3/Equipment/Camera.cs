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
using System.IO;
using NINA.Image.Interfaces;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.Equipment.Model;
using ninaAPI.WebService.V3.Handler;
using System.Windows.Media.Imaging;
using NINA.PlateSolving.Interfaces;
using NINA.Astrometry;

namespace ninaAPI.WebService.V3.Equipment
{
    class CameraInfoResponse : CameraInfo
    {
        public static CameraInfoResponse FromCam(ICameraMediator cam)
        {
            return new CameraInfoResponse()
            {
                Connected = cam.GetInfo().Connected,
                CanSetTemperature = cam.GetInfo().CanSetTemperature,
                HasDewHeater = cam.GetInfo().HasDewHeater,
                IsExposing = cam.GetInfo().IsExposing,
                PixelSize = cam.GetInfo().PixelSize,
                BinX = cam.GetInfo().BinX,
                BinY = cam.GetInfo().BinY,
                Battery = cam.GetInfo().Battery,
                Offset = cam.GetInfo().Offset,
                OffsetMin = cam.GetInfo().OffsetMin,
                OffsetMax = cam.GetInfo().OffsetMax,
                DefaultOffset = cam.GetInfo().DefaultOffset,
                USBLimit = cam.GetInfo().USBLimit,
                USBLimitMin = cam.GetInfo().USBLimitMin,
                USBLimitMax = cam.GetInfo().USBLimitMax,
                DefaultGain = cam.GetInfo().DefaultGain,
                GainMin = cam.GetInfo().GainMin,
                GainMax = cam.GetInfo().GainMax,
                CanSetGain = cam.GetInfo().CanSetGain,
                Gains = cam.GetInfo().Gains,
                CoolerOn = cam.GetInfo().CoolerOn,
                CoolerPower = cam.GetInfo().CoolerPower,
                HasShutter = cam.GetInfo().HasShutter,
                Temperature = cam.GetInfo().Temperature,
                TemperatureSetPoint = cam.GetInfo().TemperatureSetPoint,
                ReadoutModes = cam.GetInfo().ReadoutModes,
                ReadoutMode = cam.GetInfo().ReadoutMode,
                ReadoutModeForSnapImages = cam.GetInfo().ReadoutModeForSnapImages,
                ReadoutModeForNormalImages = cam.GetInfo().ReadoutModeForNormalImages,
                IsSubSampleEnabled = cam.GetInfo().IsSubSampleEnabled,
                SubSampleX = cam.GetInfo().SubSampleX,
                SubSampleY = cam.GetInfo().SubSampleY,
                SubSampleWidth = cam.GetInfo().SubSampleWidth,
                SubSampleHeight = cam.GetInfo().SubSampleHeight,
                ExposureMax = cam.GetInfo().ExposureMax,
                ExposureMin = cam.GetInfo().ExposureMin,
                LiveViewEnabled = cam.GetInfo().LiveViewEnabled,
                CanShowLiveView = cam.GetInfo().CanShowLiveView,
                SupportedActions = cam.GetInfo().SupportedActions,
                CanSetUSBLimit = cam.GetInfo().CanSetUSBLimit,
                Name = cam.GetInfo().Name,
                DisplayName = cam.GetInfo().DisplayName,
                DeviceId = cam.GetInfo().DeviceId,
                BayerOffsetX = cam.GetInfo().BayerOffsetX,
                BayerOffsetY = cam.GetInfo().BayerOffsetY,
                BinningModes = cam.GetInfo().BinningModes,
                BitDepth = cam.GetInfo().BitDepth,
                CameraState = cam.GetInfo().CameraState,
                XSize = cam.GetInfo().XSize,
                YSize = cam.GetInfo().YSize,
                CanGetGain = cam.GetInfo().CanGetGain,
                CanSetOffset = cam.GetInfo().CanSetOffset,
                CanSubSample = cam.GetInfo().CanSubSample,
                Description = cam.GetInfo().Description,
                DewHeaterOn = cam.GetInfo().DewHeaterOn,
                DriverInfo = cam.GetInfo().DriverInfo,
                DriverVersion = cam.GetInfo().DriverVersion,
                ElectronsPerADU = cam.GetInfo().ElectronsPerADU,
                ExposureEndTime = cam.GetInfo().ExposureEndTime,
                LastDownloadTime = cam.GetInfo().LastDownloadTime,
                SensorType = cam.GetInfo().SensorType,
                Gain = cam.GetInfo().Gain,
                TargetTemp = cam.TargetTemp,
                AtTargetTemp = cam.AtTargetTemp,
            };
        }
        public double TargetTemp { get; set; }
        public bool AtTargetTemp { get; set; }
    }

    public class CameraController : WebApiController
    {
        [Route(HttpVerbs.Get, "/info")]
        public async Task CameraInfo()
        {
            Logger.Info("Requesting camera info");
            object response;
            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;
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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Put, "/set-readout")]
        public async Task CameraSetReadout()
        {
            StringResponse response = new StringResponse();

            QueryParameter<int> modeParameter = new QueryParameter<int>("mode", 0, true);

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

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

            await HttpContext.WriteResponse(response);
        }


        private CancellationTokenSource CameraCoolingToken;

        [Route(HttpVerbs.Post, "/cool")]
        public async Task CameraCool()
        {
            StringResponse response = new StringResponse();

            QueryParameter<double> temperatureParameter = new QueryParameter<double>("temperature", -10, true);
            QueryParameter<double> minutesParameter = new QueryParameter<double>("minutes", AdvancedAPI.Controls.Profile.ActiveProfile.CameraSettings.CoolingDuration, false, (minutes) => minutes >= 0);
            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (!cam.GetInfo().CanSetTemperature)
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Camera has no temperature control");
                }
                else
                {
                    temperatureParameter.Get(HttpContext);
                    minutesParameter.Get(HttpContext);


                    CameraCoolingToken?.Cancel();
                    CameraCoolingToken = new CancellationTokenSource();
                    cam.CoolCamera(
                        temperatureParameter.Value,
                        TimeSpan.FromMinutes(minutesParameter.Value),
                        AdvancedAPI.Controls.StatusMediator.GetStatus(),
                        CameraCoolingToken.Token
                    );
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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Post, "/cool/abort")]
        public async Task CameraCoolAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else
                {
                    CameraCoolingToken?.Cancel();
                    response.Message = "Cooling aborted";
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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Get, "/warm")]
        public async Task CameraWarm()
        {
            StringResponse response = new StringResponse();

            QueryParameter<double> minutesParameter = new QueryParameter<double>("minutes", AdvancedAPI.Controls.Profile.ActiveProfile.CameraSettings.WarmingDuration, false);

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (!cam.GetInfo().CanSetTemperature)
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Camera has no temperature control");
                }
                else if (minutesParameter.Value < 0)
                {
                    throw CommonErrors.ParameterInvalid("minutes");
                }
                else
                {
                    minutesParameter.Get(HttpContext);

                    CameraCoolingToken?.Cancel();
                    CameraCoolingToken = new CancellationTokenSource();
                    cam.WarmCamera(
                        TimeSpan.FromMinutes(minutesParameter.Value),
                        AdvancedAPI.Controls.StatusMediator.GetStatus(),
                        CameraCoolingToken.Token
                    );
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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Post, "/warm/abort")]
        public async Task CameraWarmAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else
                {
                    CameraCoolingToken?.Cancel();
                    response.Message = "Warming aborted";
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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Post, "/abort-exposure")]
        public async Task AbortExposure()
        {
            StringResponse response = new StringResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Put, "/dew-heater")]
        public async Task CameraDewHeater()
        {
            StringResponse response = new StringResponse();

            QueryParameter<bool> powerParameter = new QueryParameter<bool>("power", false, true);

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Put, "/set-binning")]
        public async Task CameraSetBinning()
        {
            StringResponse response = new StringResponse();

            SizeQueryParameter binningParameter = new SizeQueryParameter(new Size(1, 1), true, "x", "y");

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

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

            await HttpContext.WriteResponse(response);
        }

        private volatile bool isCaptureBayered = false;
        private volatile int bitDepth = 16;
        private double pixelSize;
        private volatile Task CaptureTask;
        private CancellationTokenSource CameraCaptureToken;
        private volatile PlateSolveResult plateSolveResult;

        private object captureLock = new object();


        [Route(HttpVerbs.Post, "/capture")]
        public async Task CameraCapture()
        {
            StringResponse response = new StringResponse();
            ICameraMediator cam = AdvancedAPI.Controls.Camera;
            CameraInfo info = cam.GetInfo();

            IPlateSolveSettings settings = AdvancedAPI.Controls.Profile.ActiveProfile.PlateSolveSettings;

            QueryParameter<double> durationParameter = new QueryParameter<double>("duration", settings.ExposureTime, false, (exp) => exp > 0);
            QueryParameter<int> gainParameter = new QueryParameter<int>("gain", settings.Gain == -1 ? info.Gain : settings.Gain, false, (gain) => gain >= cam.GetInfo().GainMin && gain <= cam.GetInfo().GainMax);
            QueryParameter<bool> saveParameter = new QueryParameter<bool>("save", false, false);
            QueryParameter<double> roiParameter = new QueryParameter<double>("roi", 1.0, false, (roi) => roi > 0 && roi <= 1);

            bool taskCompleted = false;
            lock (captureLock)
            {
                taskCompleted = CaptureTask?.IsCompleted ?? true;
            }

            try
            {
                if (!taskCompleted)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
                }
                else if (!info.Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Camera);
                }
                else if (info.IsExposing)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Camera currently exposing");
                }
                else
                {
                    durationParameter.Get(HttpContext);
                    gainParameter.Get(HttpContext);
                    saveParameter.Get(HttpContext);
                    roiParameter.Get(HttpContext);

                    CameraCaptureToken?.Cancel();
                    CameraCaptureToken = new CancellationTokenSource();

                    CaptureTask = Task.Run(async () =>
                    {
                        CaptureSequence sequence = new CaptureSequence(
                            durationParameter.Value,
                            CaptureSequence.ImageTypes.SNAPSHOT,
                            AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                            new BinningMode(info.BinX, info.BinY),
                            1);

                        sequence.Gain = gainParameter.Value;

                        if (roiParameter.WasProvided)
                        {
                            if (cam.GetInfo().CanSubSample)
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
                            else
                            {
                                throw new HttpException(HttpStatusCode.Conflict, "Camera does not support sub-sampling");
                            }
                        }

                        PrepareImageParameters parameters = new PrepareImageParameters(autoStretch: true);
                        IExposureData exposure = await AdvancedAPI.Controls.Imaging.CaptureImage(sequence, CameraCaptureToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                        IRenderedImage renderedImage = await AdvancedAPI.Controls.Imaging.PrepareImage(exposure, parameters, CameraCaptureToken.Token);

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
                            await AdvancedAPI.Controls.ImageSaveMediator.Enqueue(renderedImage.RawImageData, Task.Run(() => renderedImage), AdvancedAPI.Controls.StatusMediator.GetStatus(), CameraCaptureToken.Token);
                        }
                        // TODO: Should we use IMAGE-PREPARED or API-CAPTURE-FINISHED? await WebSocketV2.SendAndAddEvent("API-CAPTURE-FINISHED");
                    }, CameraCaptureToken.Token);

                    CaptureTask.ContinueWith(t => { if (t.IsFaulted) Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);

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

            await HttpContext.WriteResponse(response);
        }

        [Route(HttpVerbs.Get, "/capture/image")]
        public async Task CameraCaptureImage()
        {
            ImageQueryParameterSet imageQuery = ImageQueryParameterSet.Default();
            imageQuery.Evaluate(HttpContext);

            try
            {
                BitmapEncoder encoder = await ImageHandler.PrepareImage(FileSystemHelper.GetCapturePngPath(), isCaptureBayered, imageQuery, bitDepth);
                HttpContext.Response.ContentType = encoder.CodecInfo.MimeTypes;
                using (MemoryStream memory = new MemoryStream())
                {
                    encoder.Save(memory);
                    await HttpContext.Response.OutputStream.WriteAsync(memory.ToArray());
                    return;
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
                else
                {
                    rawConverterParameter.Get(HttpContext);
                    starSensitivityParameter.Get(HttpContext);
                    noiseReductionParameter.Get(HttpContext);

                    IImageData imageData = await Retry.Do(
                        async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(
                            FileSystemHelper.GetCapturePngPath(),
                            bitDepth,
                            isCaptureBayered,
                            rawConverterParameter.Value
                        ),
                        TimeSpan.FromMilliseconds(200), 10
                    );
                    var img = await imageData.RenderImage().DetectStars(false, starSensitivityParameter.Value, noiseReductionParameter.Value);
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

                    await HttpContext.WriteResponse(stats);
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
        public async Task CameraCaptureSolve()
        {
            IPlateSolveSettings settings = AdvancedAPI.Controls.Profile.ActiveProfile.PlateSolveSettings;

            QueryParameter<RawConverterEnum> rawConverterParameter = new QueryParameter<RawConverterEnum>("raw-converter", RawConverterEnum.FREEIMAGE, false);
            QueryParameter<bool> blindFailoverParameter = new QueryParameter<bool>("blind-failover", settings.BlindFailoverEnabled, false);
            QueryParameter<int> downsampleFactorParameter = new QueryParameter<int>("downsample-factor", settings.DownSampleFactor, false, (downsampleFactor) => downsampleFactor >= 1);
            QueryParameter<double> searchRadiusParameter = new QueryParameter<double>("search-radius", settings.SearchRadius, false, (searchRadius) => searchRadius >= 0);

            try
            {
                if (!File.Exists(FileSystemHelper.GetCapturePngPath()))
                {
                    throw new HttpException(HttpStatusCode.Conflict, "No image available");
                }
                else if (!(CaptureTask?.IsCompleted ?? false))
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Capture in progress");
                }
                else
                {
                    if (plateSolveResult is null)
                    {
                        rawConverterParameter.Get(HttpContext);
                        blindFailoverParameter.Get(HttpContext);
                        downsampleFactorParameter.Get(HttpContext);

                        IImageData imageData = await Retry.Do(
                            async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(
                                FileSystemHelper.GetCapturePngPath(),
                                bitDepth,
                                isCaptureBayered,
                                rawConverterParameter.Value
                            ),
                            TimeSpan.FromMilliseconds(200), 10
                        );

                        IPlateSolverFactory platesolver = AdvancedAPI.Controls.PlateSolver;
                        Coordinates coordinates = AdvancedAPI.Controls.Mount.GetCurrentPosition();
                        double focalLength = AdvancedAPI.Controls.Profile.ActiveProfile.TelescopeSettings.FocalLength;
                        CaptureSolverParameter solverParameter = new CaptureSolverParameter()
                        {
                            Attempts = 1,
                            Binning = settings.Binning,
                            BlindFailoverEnabled = blindFailoverParameter.Value,
                            Coordinates = coordinates,
                            DownSampleFactor = downsampleFactorParameter.Value,
                            FocalLength = focalLength,
                            MaxObjects = settings.MaxObjects,
                            Regions = settings.Regions,
                            SearchRadius = searchRadiusParameter.Value,
                            PixelSize = pixelSize
                        };
                        IImageSolver captureSolver = platesolver.GetImageSolver(platesolver.GetPlateSolver(settings), platesolver.GetBlindSolver(settings));
                        var result = await captureSolver.Solve(imageData, solverParameter, AdvancedAPI.Controls.StatusMediator.GetStatus(), HttpContext.CancellationToken);

                        lock (captureLock)
                        {
                            plateSolveResult = result;
                        }
                    }

                    await HttpContext.WriteResponse(plateSolveResult);
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
    }
}
