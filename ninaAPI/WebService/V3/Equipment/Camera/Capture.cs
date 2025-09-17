#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.IO;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class Capture
    {
        public CaptureConfig Config { get; private set; }
        public ApiProcess CameraCaptureProcess { get; private set; }

        private readonly ICameraMediator camera;
        private readonly IProfileService profile;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IFilterWheelMediator filterWheel;
        private readonly IApplicationStatusMediator statusMediator;

        public Capture(ICameraMediator camera, FilterWheelMediator filterWheel, IProfileService profile, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IApplicationStatusMediator statusMediator)
        {
            this.camera = camera;
            this.filterWheel = filterWheel;
            this.profile = profile;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.statusMediator = statusMediator;
        }

        private volatile bool isCaptureBayered = false;
        private volatile int bitDepth = 16;
        private double pixelSize;
        private volatile PlateSolveResult plateSolveResult;

        private object captureLock = new object();

        public async Task Start(CaptureConfig config)
        {
            if (CameraCaptureProcess != null && CameraCaptureProcess.Status == ApiProcessStatus.Running)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Capture already running");
            }
            CameraCaptureProcess = new ApiProcess(async (token) =>
            {
                CaptureSequence sequence = new CaptureSequence(
                    (double)config.Duration,
                    CaptureSequence.ImageTypes.SNAPSHOT,
                    filterWheel.GetInfo().SelectedFilter,
                    config.Binning,
                    1);

                sequence.Gain = (int)config.Gain;

                if (config.ROI < 1)
                {
                    var info = camera.GetInfo();
                    var centerX = info.XSize / 2d;
                    var centerY = info.YSize / 2d;
                    double subWidth = info.XSize * (double)config.ROI;
                    double subHeight = info.YSize * (double)config.ROI;
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

                if (config.Save ?? false)
                {
                    await imageSaveMediator.Enqueue(renderedImage.RawImageData, Task.Run(() => renderedImage), statusMediator.GetStatus(), token);
                }
                // TODO: Should we use IMAGE-PREPARED or API-CAPTURE-FINISHED? await WebSocketV2.SendAndAddEvent("API-CAPTURE-FINISHED");
            });
            CameraCaptureProcess.Start();
        }
    }
}