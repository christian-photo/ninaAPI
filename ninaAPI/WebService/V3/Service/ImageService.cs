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
                // ImageFormat.AVIF => new AvifWriter(image),
                // ImageFormat.HEIF => new HeifWriter(image),
                // ImageFormat.JPEGXL => new JpegXlWriter(image),
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
            if (parameters.Stretch.Value)
            {
                image = await image.Stretch(parameters.StretchFactor.Value, parameters.BlackClipping.Value, parameters.UnlinkedStretch.Value);
            }
            return image;
        }
    }

    public abstract class ImageWriter(BitmapSource img)
    {
        protected BitmapSource image = img;

        public abstract byte[] Encode(ImageQueryParameterSet parameter);
        public abstract string MimeType { get; protected set; }

        protected static SKImage BitmapSourceToSkImage(BitmapSource bitmapSource)
        {
            if (bitmapSource.Format != System.Windows.Media.PixelFormats.Bgra32)
            {
                var converted = new FormatConvertedBitmap();
                converted.BeginInit();
                converted.Source = bitmapSource;
                converted.DestinationFormat = System.Windows.Media.PixelFormats.Bgra32;
                converted.EndInit();
                bitmapSource = converted;
            }

            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;

            var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var ptr = bitmap.GetPixels();
            bitmapSource.CopyPixels(new System.Windows.Int32Rect(0, 0, width, height),
                                    ptr, bitmap.ByteCount, bitmap.RowBytes);

            return SKImage.FromBitmap(bitmap);
        }
    }

    public class JpegWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(ImageQueryParameterSet parameter)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = parameter.Quality.Value;
            encoder.Frames.Add(BitmapFrame.Create(image));

            using var target = new MemoryStream();
            encoder.Save(target);
            return target.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/jpeg";
    }

    public class PngWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(ImageQueryParameterSet parameter)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/png";
    }

    public class WebpWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(ImageQueryParameterSet parameter)
        {
            int quality = parameter.Quality.Value;

            using var skImage = BitmapSourceToSkImage(image);
            using var data = skImage.Encode(SKEncodedImageFormat.Webp, quality);

            using var target = new MemoryStream();
            data.SaveTo(target);
            return target.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/webp";
    }

    // public class HeifWriter(BitmapSource image) : ImageWriter(image)
    // {
    //     public override byte[] Encode(ImageQueryParameterSet parameter)
    //     {
    //         int quality = parameter.Quality.Value;

    //         using var skImage = BitmapSourceToSkImage(image);
    //         using var data = skImage.Encode(SKEncodedImageFormat.Heif, quality);

    //         using var target = new MemoryStream();
    //         data.SaveTo(target);
    //         return target.ToArray();
    //     }

    //     public override string MimeType { get; protected set; } = "image/heif";
    // }

    // public class AvifWriter(BitmapSource image) : ImageWriter(image)
    // {
    //     public override byte[] Encode(ImageQueryParameterSet parameter)
    //     {
    //         int quality = parameter.Quality.Value;

    //         using var skImage = BitmapSourceToSkImage(image);
    //         using var data = skImage.Encode(SKEncodedImageFormat.Avif, quality);

    //         using var target = new MemoryStream();
    //         data.SaveTo(target);
    //         return target.ToArray();
    //     }

    //     public override string MimeType { get; protected set; } = "image/avif";
    // }

    // public class JpegXlWriter(BitmapSource image) : ImageWriter(image)
    // {
    //     public override byte[] Encode(ImageQueryParameterSet parameter)
    //     {
    //         int quality = parameter.Quality.Value;

    //         using var skImage = BitmapSourceToSkImage(image);
    //         using var data = skImage.Encode(SKEncodedImageFormat.Jpegxl, quality);

    //         using var target = new MemoryStream();
    //         data.SaveTo(target);
    //         return target.ToArray();
    //     }

    //     public override string MimeType { get; protected set; } = "image/jxl";
    // }
}
