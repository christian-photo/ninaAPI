#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
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

namespace ninaAPI.WebService.V2
{
    class CaptureResponse
    {
        public string Image { get; set; }
        public PlateSolveResult PlateSolveResult { get; set; }
    }

    public partial class ControllerV2
    {
        public static void StartCameraWatchers()
        {
            AdvancedAPI.Controls.Camera.Connected += async (_, _) => await WebSocketV2.SendAndAddEvent("CAMERA-CONNECTED");
            AdvancedAPI.Controls.Camera.Disconnected += async (_, _) => await WebSocketV2.SendAndAddEvent("CAMERA-DISCONNECTED");
            AdvancedAPI.Controls.Camera.DownloadTimeout += async (_, _) => await WebSocketV2.SendAndAddEvent("CAMERA-DOWNLOAD-TIMEOUT");
        }
        private static CancellationTokenSource CameraCaptureToken;
        private static PlateSolveResult plateSolveResult;
        private static IRenderedImage renderedImage;
        private static Task CaptureTask;

        private static CancellationTokenSource CameraCoolToken;


        [Route(HttpVerbs.Get, "/equipment/camera/info")]
        public void CameraInfo()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;
                response.Response = cam.GetInfo();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/connect")]
        public async Task CameraConnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (!cam.GetInfo().Connected)
                {
                    await cam.Rescan();
                    await cam.Connect();
                }
                response.Response = "Camera connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/camera/disconnect")]
        public async Task CameraDisconnect()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;

                if (cam.GetInfo().Connected)
                {
                    await cam.Disconnect();
                }
                response.Response = "Camera disconnected";
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
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
                        cam.CoolCamera(temperature, TimeSpan.FromMinutes(minutes), AdvancedAPI.Controls.StatusMediator.GetStatus(), CameraCoolToken.Token);
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
        public void CameraWarm([QueryField] bool cancel)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
                        cam.WarmCamera(TimeSpan.Zero, AdvancedAPI.Controls.StatusMediator.GetStatus(), CameraCoolToken.Token);
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
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
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

        [Route(HttpVerbs.Get, "/equipment/camera/capture")]
        public void CameraCapture([QueryField] bool solve, [QueryField] float duration, [QueryField] bool getResult, [QueryField] bool resize, [QueryField] int quality, [QueryField] string size, [QueryField] int gain, [QueryField] double scale)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

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
                    string image = string.Empty;
                    if (scale == 0 && resize)
                        image = BitmapHelper.ResizeAndConvertBitmap(renderedImage.Image, resolution, quality);
                    if (scale != 0 && resize)
                        image = BitmapHelper.ScaleAndConvertBitmap(renderedImage.Image, scale, quality);
                    if (!resize)
                        image = BitmapHelper.ScaleAndConvertBitmap(renderedImage.Image, 1, quality);

                    response.Response = new CaptureResponse() { Image = image, PlateSolveResult = plateSolveResult };
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
                    CameraCaptureToken?.Cancel();
                    CameraCaptureToken = new CancellationTokenSource();

                    CaptureTask = Task.Run(async () =>
                    {
                        renderedImage = null;
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
                        IExposureData exposure = await AdvancedAPI.Controls.Imaging.CaptureImage(sequence, CameraCaptureToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                        renderedImage = await AdvancedAPI.Controls.Imaging.PrepareImage(exposure, parameters, CameraCaptureToken.Token);


                        if (solve)
                        {
                            IPlateSolverFactory platesolver = AdvancedAPI.Controls.PlateSolver;
                            Coordinates coordinates = AdvancedAPI.Controls.Mount.GetCurrentPosition();
                            double focalLength = AdvancedAPI.Controls.Profile.ActiveProfile.TelescopeSettings.FocalLength;
                            double pixelSize = cam.GetInfo().PixelSize;
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
                                PixelSize = cam.GetInfo().PixelSize
                            };
                            IImageSolver captureSolver = platesolver.GetImageSolver(platesolver.GetPlateSolver(settings), platesolver.GetBlindSolver(settings));
                            plateSolveResult = await captureSolver.Solve(renderedImage.RawImageData, solverParameter, AdvancedAPI.Controls.StatusMediator.GetStatus(), CameraCaptureToken.Token);
                        }
                    }, CameraCaptureToken.Token);

                    response.Response = "Capture started";
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
