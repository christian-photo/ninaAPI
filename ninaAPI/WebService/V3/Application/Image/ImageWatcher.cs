#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Model;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Application.Image
{
    public class ImageWatcher : EventWatcher
    {
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImagingMediator imagingMediator;

        public ImageWatcher(EventHistoryManager eventHistory, IImageSaveMediator imageSaveMediator, IImagingMediator imagingMediator) : base(eventHistory)
        {
            this.imageSaveMediator = imageSaveMediator;
            this.imagingMediator = imagingMediator;

            Channel = WebSocketChannel.Image;
        }

        private readonly static List<ImageResponse> history = [];
        public static IRenderedImage PreparedImage { get; private set; }

        public override void StartWatchers()
        {
            imageSaveMediator.ImageSaved += ImageSaved;
            imagingMediator.ImagePrepared += ImagePrepared;
        }

        public override void StopWatchers()
        {
            imageSaveMediator.ImageSaved -= ImageSaved;
            imagingMediator.ImagePrepared += ImagePrepared;
        }

        private static readonly object imageLock = new();

        private async void ImageSaved(object sender, ImageSavedEventArgs e)
        {
            var imageInfo = ImageResponse.FromEvent(e);

            imageInfo.SetThumbnailPath(CacheThumbnail(e));

            lock (imageLock)
            {
                history.Add(imageInfo);
            }

            await SubmitEvent(WebSocketEvents.IMAGE_SAVED, imageInfo);
        }

        private static string CacheThumbnail(ImageSavedEventArgs e)
        {
            if (!Properties.Settings.Default.CreateThumbnails)
                return string.Empty;

            lock (imageLock)
            {
                string thumbnailFile = FileSystemHelper.GetThumbnailFolder();
                Directory.CreateDirectory(thumbnailFile);
                thumbnailFile = Path.Combine(thumbnailFile, $"{history.Count - 1}.png");

                // TODO: Make the thumbnail configurable (either scale or long axis dimension)
                var img = BitmapHelper.ScaleBitmap(e.Image, 256 / e.Image.Width);

                // Encode as png to minimize quality loss, the small dimensions should already be sufficient for decreasing the file size
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(img));

                using (FileStream fs = new FileStream(thumbnailFile, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                return thumbnailFile;
            }
        }

        public static IReadOnlyList<ImageResponse> GetImageHistory()
        {
            lock (imageLock)
            {
                return [.. history];
            }
        }

        private async void ImagePrepared(object sender, ImagePreparedEventArgs e)
        {
            lock (imageLock)
            {
                PreparedImage = e.RenderedImage;
            }

            await SubmitEvent(WebSocketEvents.IMAGE_PREPARED);
        }
    }
}
