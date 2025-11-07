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
using System.Threading.Tasks;
using EmbedIO.Routing;
using System.Drawing;
using System.Threading;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using System.Linq;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Generic;
using NINA.Image.ImageData;
using NINA.Image.ImageAnalysis;
using NINA.Core.Enum;
using System.Reflection;

namespace ninaAPI.WebService.V2
{
    class CaptureResponse
    {
        public string Image { get; set; }
        public PlateSolveResult PlateSolveResult { get; set; }
    }

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
                offsetMax = cam.GetInfo().offsetMax,
                offsetMin = cam.GetInfo().offsetMin,
                TargetTemp = cam.TargetTemp,
                AtTargetTemp = cam.GetInfo().Temperature == cam.GetInfo().TemperatureSetPoint,
            };
        }
        public double TargetTemp { get; set; }
        public bool AtTargetTemp { get; set; }
    }

    public class CameraWatcher : INinaWatcher, ICameraConsumer
    {
        private readonly Func<object, EventArgs, Task> CameraConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("CAMERA-CONNECTED");
        private readonly Func<object, EventArgs, Task> CameraDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("CAMERA-DISCONNECTED");
        private readonly Func<object, EventArgs, Task> CameraDownloadTimeoutHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("CAMERA-DOWNLOAD-TIMEOUT");

        public void Dispose()
        {
            AdvancedAPI.Controls.Camera.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Camera.Connected += CameraConnectedHandler;
            AdvancedAPI.Controls.Camera.Disconnected += CameraDisconnectedHandler;
            AdvancedAPI.Controls.Camera.DownloadTimeout += CameraDownloadTimeoutHandler;
            AdvancedAPI.Controls.Camera.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Camera.Connected -= CameraConnectedHandler;
            AdvancedAPI.Controls.Camera.Disconnected -= CameraDisconnectedHandler;
            AdvancedAPI.Controls.Camera.DownloadTimeout -= CameraDownloadTimeoutHandler;
            AdvancedAPI.Controls.Camera.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(CameraInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("CAMERA");
        }
    }

    public partial class ControllerV2
    {
        private static PlateSolveResult plateSolveResult;
        private static bool isBayered;
        private static Task CaptureTask;

        private static CancellationTokenSource CameraCoolToken;


        [Route(HttpVerbs.Get, "/equipment/camera/info")]
        public void CameraInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;
                CameraInfoResponse info = CameraInfoResponse.FromCam(cam);
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/set-readout")]
        public void CameraSetReadout([QueryField] short mode)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (mode >= 0 && mode < cam.GetInfo().ReadoutModes.Count())
                {
                    cam.SetReadoutMode(mode);
                    response.Response = "Readout mode updated";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Invalid readout mode", 400));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/cool")]
        public void CameraCool([QueryField] double temperature, [QueryField] bool cancel, [QueryField] double minutes)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (!cam.GetInfo().CanSetTemperature)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera has no temperature control", 409));
                }
                else
                {
                    if (cancel)
                    {
                        CameraCoolToken?.Cancel();
                        response.Response = "Cooling canceled";
                    }
                    else
                    {
                        CameraCoolToken?.Cancel();
                        CameraCoolToken = new CancellationTokenSource();
                        cam.CoolCamera(temperature, TimeSpan.FromMinutes(minutes == -1 ? AdvancedAPI.Controls.Profile.ActiveProfile.CameraSettings.CoolingDuration : minutes), AdvancedAPI.Controls.StatusMediator.GetStatus(), CameraCoolToken.Token);
                        response.Response = "Cooling started";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/warm")]
        public void CameraWarm([QueryField] bool cancel, [QueryField] double minutes)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (!cam.GetInfo().CanSetTemperature)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera has no temperature control", 409));
                }
                else
                {
                    if (cancel)
                    {
                        CameraCoolToken?.Cancel();
                        response.Response = "Warming canceled";
                    }
                    else
                    {
                        CameraCoolToken?.Cancel();
                        CameraCoolToken = new CancellationTokenSource();
                        cam.WarmCamera(TimeSpan.FromMinutes(minutes == -1 ? AdvancedAPI.Controls.Profile.ActiveProfile.CameraSettings.WarmingDuration : minutes), AdvancedAPI.Controls.StatusMediator.GetStatus(), CameraCoolToken.Token);
                        response.Response = "Warming started";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/abort-exposure")]
        public void AbortExposure()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (cam.GetInfo().IsExposing)
                {
                    response.Response = "Exposure aborted";
                    cam.AbortExposure();
                }
                else
                {
                    response.Response = "Camera not exposing";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/dew-heater")]
        public void CameraDewHeater([QueryField] bool power)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (!cam.GetInfo().HasDewHeater)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera has no dew heater", 409));
                }
                else
                {
                    cam.SetDewHeater(power);
                    response.Response = "Dew heater set";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/set-binning")]
        public void CameraSetBinning([QueryField] string binning)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (string.IsNullOrEmpty(binning))
                {
                    response = CoreUtility.CreateErrorTable(new Error("Binning must be specified", 409));
                }
                else if (!cam.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (binning.Contains('x'))
                {
                    string[] parts = binning.Split('x');
                    if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                    {
                        BinningMode mode = cam.GetInfo().BinningModes.FirstOrDefault(b => b.X == x && b.Y == y, null);
                        if (mode == null)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Invalid binning mode", 409));
                        }
                        else
                        {
                            cam.SetBinning(mode.X, mode.Y);
                            response.Response = "Binning set";
                        }
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid binning mode", 409));
                    }
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Binning must be specified", 409));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/capture/statistics")]
        public async Task CameraCaptureStats()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp.png")))
                {
                    response = CoreUtility.CreateErrorTable(new Error("No image available", 400));
                }
                else
                {
                    IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp.png"), 16, isBayered, RawConverterEnum.FREEIMAGE), TimeSpan.FromMilliseconds(200), 10);
                    var img = await imageData.RenderImage().DetectStars(false, StarSensitivityEnum.Normal, NoiseReductionEnum.None);
                    var s = ImageStatistics.Create(img.RawImageData);

                    Dictionary<string, object> stats = new Dictionary<string, object>() {
                        { "Stars", img.RawImageData.StarDetectionAnalysis.DetectedStars },
                        { "HFR", img.RawImageData.StarDetectionAnalysis.HFR },
                        { "Median", s.Median },
                        { "MedianAbsoluteDeviation", s.MedianAbsoluteDeviation },
                        { "Mean", s.Mean },
                        { "Max", s.Max },
                        { "Min", s.Min },
                        { "StDev", s.StDev },
                };

                    response.Response = stats;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/capture")]
        public async Task CameraCapture(
            [QueryField] bool solve,
            [QueryField] float duration,
            [QueryField] bool getResult,
            [QueryField] bool resize,
            [QueryField] int quality,
            [QueryField] string size,
            [QueryField] int gain,
            [QueryField] double scale,
            [QueryField] bool stream,
            [QueryField] bool omitImage,
            [QueryField] bool waitForResult,
            [QueryField] bool save,
            [QueryField] string targetName,
            [QueryField] bool onlyAwaitCaptureCompletion,
            [QueryField] bool onlySaveRaw)
        {

            HttpResponse response = new HttpResponse();
            ICameraMediator cam = AdvancedAPI.Controls.Camera;

            quality = Math.Clamp(quality, -1, 100);
            if (quality == 0)
                quality = -1; // quality should be set to -1 for png if omitted

            if (resize && string.IsNullOrWhiteSpace(size)) // default value for size is 640x480
                size = "640x480";

            Size resolution = Size.Empty;

            try
            {
                if (resize)
                {
                    string[] s = size.Split('x');
                    int width = int.Parse(s[0]);
                    int height = int.Parse(s[1]);
                    resolution = new Size(width, height);
                }

                // wants result, task is running => error
                // wants result, task is not running => result
                // wants capture, camera exposing => error
                if (getResult && CaptureTask is null)
                {
                    response = CoreUtility.CreateErrorTable(new Error("No capture processed", 409));
                }
                else if (getResult && !CaptureTask.IsCompleted)
                {
                    response.Response = "Capture already in progress";
                }
                else if (getResult && CaptureTask.IsCompleted)
                {
                    Bitmap img = new Bitmap(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp.png"));
                    BitmapSource source = ImageUtility.ConvertBitmap(img);
                    if (stream)
                    {
                        BitmapEncoder encoder = null;
                        if (scale == 0 && resize)
                        {
                            BitmapSource image = BitmapHelper.ResizeBitmap(source, resolution);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        if (scale != 0 && resize)
                        {
                            BitmapSource image = BitmapHelper.ScaleBitmap(source, scale);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        if (!resize)
                        {
                            BitmapSource image = BitmapHelper.ScaleBitmap(source, 1);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        HttpContext.Response.ContentType = quality == -1 ? "image/png" : "image/jpg";
                        using (MemoryStream memory = new MemoryStream())
                        {
                            encoder.Save(memory);
                            await HttpContext.Response.OutputStream.WriteAsync(memory.ToArray());
                            return;
                        }
                    }
                    else
                    {
                        if (!omitImage)
                        {
                            string image = string.Empty;
                            if (scale == 0 && resize)
                                image = BitmapHelper.ResizeAndConvertBitmap(source, resolution, quality);
                            if (scale != 0 && resize)
                                image = BitmapHelper.ScaleAndConvertBitmap(source, scale, quality);
                            if (!resize)
                                image = BitmapHelper.ScaleAndConvertBitmap(source, 1, quality);

                            response.Response = new CaptureResponse() { Image = image, PlateSolveResult = plateSolveResult };
                        }
                        else
                        {
                            response.Response = new CaptureResponse() { PlateSolveResult = plateSolveResult, Image = null };
                        }
                    }
                }
                else if (!getResult && !cam.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (!getResult && cam.GetInfo().IsExposing)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera currently exposing", 409));
                }
                else
                {
                    CaptureTask = Task.Run(async () =>
                    {
                        plateSolveResult = null;
                        IPlateSolveSettings settings = AdvancedAPI.Controls.Profile.ActiveProfile.PlateSolveSettings;

                        CaptureSequence sequence = new CaptureSequence(
                            duration <= 0 ? settings.ExposureTime : duration,
                            CaptureSequence.ImageTypes.SNAPSHOT,
                            AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                            new BinningMode(cam.GetInfo().BinX, cam.GetInfo().BinY),
                            1);

                        if (gain > 0)
                        {
                            sequence.Gain = gain;
                        }

                        PrepareImageParameters parameters = new PrepareImageParameters(autoStretch: true);
                        IExposureData exposure = await AdvancedAPI.Controls.Imaging.CaptureImage(
                            sequence,
                            CancellationToken.None,
                            AdvancedAPI.Controls.StatusMediator.GetStatus(),
                            string.IsNullOrEmpty(targetName) ? "Snapshot" : targetName
                        );
                        IRenderedImage renderedImage = await AdvancedAPI.Controls.Imaging.PrepareImage(exposure, parameters, CancellationToken.None);

                        if (!onlySaveRaw)
                        {
                            var encoder = BitmapHelper.GetEncoder(renderedImage.Image, -1);
                            using (FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp.png"), FileMode.Create))
                            {
                                encoder.Save(fs);
                            }
                        }

                        isBayered = renderedImage.RawImageData.Properties.IsBayered;


                        if (solve)
                        {
                            IPlateSolverFactory platesolver = AdvancedAPI.Controls.PlateSolver;
                            Coordinates coordinates = AdvancedAPI.Controls.Mount.GetCurrentPosition();
                            double focalLength = AdvancedAPI.Controls.Profile.ActiveProfile.TelescopeSettings.FocalLength;
                            CaptureSolverParameter solverParameter = new CaptureSolverParameter()
                            {
                                Attempts = 1,
                                Binning = settings.Binning,
                                BlindFailoverEnabled = settings.BlindFailoverEnabled,
                                Coordinates = coordinates,
                                DownSampleFactor = settings.DownSampleFactor,
                                FocalLength = focalLength,
                                MaxObjects = settings.MaxObjects,
                                Regions = settings.Regions,
                                SearchRadius = settings.SearchRadius,
                                PixelSize = exposure.MetaData.Camera.PixelSize
                            };
                            IImageSolver captureSolver = platesolver.GetImageSolver(platesolver.GetPlateSolver(settings), platesolver.GetBlindSolver(settings));

                            plateSolveResult = await captureSolver.Solve(renderedImage.RawImageData, solverParameter, AdvancedAPI.Controls.StatusMediator.GetStatus(), CancellationToken.None);
                        }
                        if (save || onlySaveRaw)
                        {
                            await AdvancedAPI.Controls.ImageSaveMediator.Enqueue(renderedImage.RawImageData, Task.Run(() => renderedImage), AdvancedAPI.Controls.StatusMediator.GetStatus(), CancellationToken.None);
                        }
                        await WebSocketV2.SendAndAddEvent("API-CAPTURE-FINISHED");
                    }, CancellationToken.None);

                    response.Response = "Capture started";

                    if (onlyAwaitCaptureCompletion)
                    {
                        while (!CaptureTask.IsCompleted && !cam.GetInfo().IsExposing)
                        {
                            await Task.Delay(10);
                        }
                        while (!CaptureTask.IsCompleted && cam.GetInfo().IsExposing)
                        {
                            await Task.Delay(10);
                        }
                        response.Response = "Capture finished";
                    }

                    if (waitForResult)
                    {
                        await CaptureTask;
                        // Return the captured image
                        await CameraCapture(false, 0, true, resize, quality, size, 0, scale, stream, omitImage, false, false, targetName, false, onlySaveRaw);
                        return;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
