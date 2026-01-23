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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using SkiaSharp;

namespace ninaAPI.WebService.V3.Service
{
    public static class ImageService
    {
        private static ImageWriter GetImageWriter(BitmapSource image, ImageFormat format)
        {
            return format switch
            {
                ImageFormat.AVIF => new AvifWriter(image),
                ImageFormat.JPEG => new JpegWriter(image),
                ImageFormat.PNG => new PngWriter(image),
                ImageFormat.WEBP => new WebpWriter(image),
                _ => throw new NotImplementedException(),
            };
        }

        public static async Task<ImageWriter> ProcessAndPrepareImage(IRenderedImage image, ImageQueryParameterSet parameters)
        {
            image = await StretchAndDebayer(image, parameters);

            BitmapSource bitmap = ResizeBitmap(image.Image, parameters);

            return GetImageWriter(bitmap, parameters.Format.Value);
        }

        public static async Task<ImageWriter> ProcessAndPrepareImage(string path, bool isBayered, ImageQueryParameterSet parameters, int bitDepth = 16, int delay = 200, int retries = 10)
        {
            IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(path, bitDepth, isBayered, parameters.RawConverter.Value), TimeSpan.FromMilliseconds(delay), retries);
            IRenderedImage image = imageData.RenderImage();
            return await ProcessAndPrepareImage(image, parameters);
        }

        public static BitmapSource ResizeBitmap(BitmapSource image, ImageQueryParameterSet parameters)
        {
            if (parameters.Scale.WasProvided)
            {
                image = BitmapHelper.ScaleBitmap(image, parameters.Scale.Value);
            }
            else if (parameters.Size.WasProvided)
            {
                image = BitmapHelper.ResizeBitmap(image, parameters.Size.Value);
            }
            return image;
        }

        public static async Task<IRenderedImage> StretchAndDebayer(IRenderedImage image, ImageQueryParameterSet parameters)
        {
            if (parameters.Debayer.Value)
            {
                image = image.Debayer(bayerPattern: parameters.BayerPattern.Value, saveColorChannels: true, saveLumChannel: true);
            }
            return await image.Stretch(parameters.StretchFactor.Value, parameters.BlackClipping.Value, parameters.UnlinkedStretch.Value);
        }
    }

    public abstract class ImageWriter(BitmapSource img)
    {
        protected BitmapSource image = img;

        public abstract void WriteToStream(ImageQueryParameterSet parameter, Stream target);
        public abstract string MimeType { get; protected set; }
    }

    public class JpegWriter(BitmapSource image) : ImageWriter(image)
    {
        public override void WriteToStream(ImageQueryParameterSet parameter, Stream target)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = parameter.Quality.Value;
            encoder.Frames.Add(BitmapFrame.Create(image));

            encoder.Save(target);
        }

        public override string MimeType { get; protected set; } = "image/jpeg";
    }

    public class PngWriter(BitmapSource image) : ImageWriter(image)
    {
        public override void WriteToStream(ImageQueryParameterSet parameter, Stream target)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            encoder.Save(target);
        }

        public override string MimeType { get; protected set; } = "image/png";
    }

    public class WebpWriter(BitmapSource image) : ImageWriter(image)
    {
        public override void WriteToStream(ImageQueryParameterSet parameter, Stream target)
        {
            int quality = parameter.Quality.Value;

            using (var pngStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(pngStream);
                pngStream.Position = 0;

                using var skBitmap = SKBitmap.Decode(pngStream);
                using var skImage = SKImage.FromBitmap(skBitmap);
                using var data = skImage.Encode(SKEncodedImageFormat.Webp, quality);

                data.SaveTo(target);
            }
        }

        public override string MimeType { get; protected set; } = "image/jpeg";
    }

    public class AvifWriter(BitmapSource image) : ImageWriter(image)
    {
        public override void WriteToStream(ImageQueryParameterSet parameter, Stream target)
        {
            int quality = parameter.Quality.Value;

            using (var pngStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(pngStream);
                pngStream.Position = 0;

                using var skBitmap = SKBitmap.Decode(pngStream);
                using var skImage = SKImage.FromBitmap(skBitmap);
                using var data = skImage.Encode(SKEncodedImageFormat.Avif, quality);

                data.SaveTo(target);
            }
        }

        public override string MimeType { get; protected set; } = "image/avif";
    }
}
