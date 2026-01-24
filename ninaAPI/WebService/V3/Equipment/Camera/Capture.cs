#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.FileFormat;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Model;
using ninaAPI.WebService.V3.Service;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class Capture
    {
        public CaptureConfig Config { get; private set; }
        public Guid CaptureId { get; private set; }
        public Guid CaptureFinalizeProcessId { get; private set; }

        private readonly ICameraMediator camera;
        private readonly IProfileService profile;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IFilterWheelMediator filterWheel;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ApiProcessMediator processMediator;

        public Capture(ICameraMediator camera, IFilterWheelMediator filterWheel, IProfileService profile, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IApplicationStatusMediator statusMediator, ApiProcessMediator processMediator)
        {
            this.camera = camera;
            this.filterWheel = filterWheel;
            this.profile = profile;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.statusMediator = statusMediator;
            this.processMediator = processMediator;

            IExposureData exposure = null;

            CaptureId = processMediator.AddProcess(async (token) =>
            {
                CaptureSequence sequence = new CaptureSequence(
                    (double)Config.Duration,
                    Config.ImageType,
                    filterWheel.GetInfo().SelectedFilter,
                    Config.Binning,
                    1);

                sequence.Gain = (int)Config.Gain;

                if (Config.ROI < 1)
                {
                    var info = camera.GetInfo();
                    var centerX = info.XSize / 2d;
                    var centerY = info.YSize / 2d;
                    double subWidth = info.XSize * (double)Config.ROI;
                    double subHeight = info.YSize * (double)Config.ROI;
                    var startX = centerX - subWidth / 2d;
                    var startY = centerY - subHeight / 2d;
                    var rect = new ObservableRectangle(startX, startY, subWidth, subHeight);
                    sequence.EnableSubSample = true;
                    sequence.SubSambleRectangle = rect;
                }

                exposure = await imagingMediator.CaptureImage(sequence, token, statusMediator.GetStatus());

                processMediator.Start(CaptureFinalizeProcessId);
            }, ApiProcessType.CameraCapture);

            CaptureFinalizeProcessId = processMediator.AddProcess(async (token) =>
            {
                IImageData imageData = await exposure.ToImageData(statusMediator.GetStatus(), token);
                lock (captureLock)
                {
                    BitDepth = exposure.BitDepth;
                    PixelSize = exposure.MetaData.Camera.PixelSize;
                    IsCaptureBayered = imageData.Properties.IsBayered;
                    RecordedRMS = exposure.MetaData.Image.RecordedRMS.Total;
                }

                capturePath = await imageData.SaveToDisk(new FileSaveInfo(profile), token);
            }, ApiProcessType.CapturePrepare);
        }

        public bool IsCaptureBayered { get; private set; } = false;
        public int BitDepth { get; private set; } = 16;
        public double PixelSize { get; private set; }
        public double RecordedRMS { get; private set; }

        private volatile PlateSolveResult plateSolveResult;
        private volatile CaptureAnalysis captureAnalysis;

        private readonly object captureLock = new();

        public ApiProcessStartResult Start(CaptureConfig config)
        {
            Config = config;
            return processMediator.Start(CaptureId);
        }

        public bool Stop()
        {
            return processMediator.Stop(CaptureId);
        }

        /// <summary>
        /// Performs a platesolve on the captured image, this requires the image to be ready. The result is cached for future calls.
        /// </summary>
        /// <param name="imageFactory"></param>
        /// <param name="plateSolverFactory"></param>
        /// <param name="config"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<PlateSolveResult> GetPlateSolve(IImageDataFactory imageFactory, IPlateSolverFactory plateSolverFactory, PlatesolveConfig config, CancellationToken token)
        {
            if (plateSolveResult is not null)
            {
                return plateSolveResult;
            }
            Coordinates coordinates = config.Coordinates.ToCoordinates();
            var result = await new PlateSolveService(
                imageFactory,
                plateSolverFactory,
                profile.ActiveProfile.PlateSolveSettings,
                statusMediator)
                .PlateSolve(
                    GetCapturePath(),
                    config,
                    PixelSize,
                    coordinates,
                    token,
                    BitDepth,
                    IsCaptureBayered);

            lock (captureLock)
            {
                plateSolveResult = result;
            }

            return plateSolveResult;
        }

        public async Task<object> Analyze(IImageDataFactory imageFactory, StarSensitivityEnum starSensitivity, NoiseReductionEnum noiseReduction, RawConverterEnum rawConverter, CancellationToken cts)
        {
            if (captureAnalysis is not null)
            {
                return captureAnalysis;
            }
            IImageData imageData = await Retry.Do(
                async () => await imageFactory.CreateFromFile(
                    GetCapturePath(),
                    BitDepth,
                    IsCaptureBayered,
                    rawConverter
                ),
                TimeSpan.FromMilliseconds(200), 10
            );

            var img = await imageData.RenderImage().DetectStars(false, starSensitivity, noiseReduction, cts);
            var s = ImageStatistics.Create(img.RawImageData);

            lock (captureLock)
            {
                captureAnalysis = new CaptureAnalysis()
                {
                    Stars = img.RawImageData.StarDetectionAnalysis.DetectedStars,
                    HFR = img.RawImageData.StarDetectionAnalysis.HFR,
                    Median = s.Median,
                    MedianAbsoluteDeviation = s.MedianAbsoluteDeviation,
                    Mean = s.Mean,
                    Max = s.Max,
                    Min = s.Min,
                    StDev = s.StDev,
                    PixelSize = PixelSize,
                    RMS = RecordedRMS,
                    BitDepth = img.RawImageData.Properties.BitDepth,
                    Width = img.RawImageData.Properties.Width,
                    Height = img.RawImageData.Properties.Height,
                };
            }

            return captureAnalysis;
        }

        public ApiProcess GetCaptureProcess()
        {
            return processMediator.GetProcess(CaptureId, out var process) ? process : null;
        }

        public ApiProcess GetCaptureFinalizeProcess()
        {
            return processMediator.GetProcess(CaptureFinalizeProcessId, out var process) ? process : null;
        }

        private string capturePath;
        public string GetCapturePath()
        {
            return capturePath;
        }

        /// <summary>
        /// Deletes the captured image, the capture object is no longer useful after this call.
        /// </summary>
        public void Cleanup()
        {
            if (File.Exists(GetCapturePath()))
            {
                File.Delete(GetCapturePath());
            }
        }
    }

    public class CaptureAnalysis
    {
        public double Stars { get; set; }
        public double HFR { get; set; }
        public double Median { get; set; }
        public double MedianAbsoluteDeviation { get; set; }
        public double Mean { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double StDev { get; set; }
        public double RMS { get; set; }
        public double PixelSize { get; set; }
        public int BitDepth { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
