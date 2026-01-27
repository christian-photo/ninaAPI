#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Service
{
    public abstract class ImageWriter(BitmapSource img)
    {
        // TODO: Check if the larger package size is worth the small improvement in transfer speeds
        public static ImageWriter GetImageWriter(BitmapSource image, ImageFormat format)
        {
            return format switch
            {
                ImageFormat.AVIF => new AvifWriter(image),
                ImageFormat.JXL => new JxlWriter(image),
                ImageFormat.JPEG => new JpegWriter(image),
                ImageFormat.PNG => new PngWriter(image),
                ImageFormat.WEBP => new WebpWriter(image),
                _ => throw new NotImplementedException(),
            };
        }


        protected BitmapSource image = img;

        public abstract byte[] Encode(ImageQueryParameterSet parameter);
        public abstract string MimeType { get; protected set; }

        protected static byte[] BitmapSourceToByteArray(BitmapSource bitmapSource, out int width, out int height)
        {
            // Ensure the bitmap is in BGRA32 format
            if (bitmapSource.Format != System.Windows.Media.PixelFormats.Bgra32)
            {
                var converted = new FormatConvertedBitmap();
                converted.BeginInit();
                converted.Source = bitmapSource;
                converted.DestinationFormat = System.Windows.Media.PixelFormats.Bgra32;
                converted.EndInit();
                bitmapSource = converted;
            }

            width = bitmapSource.PixelWidth;
            height = bitmapSource.PixelHeight;
            int stride = width * 4; // 4 bytes per pixel (BGRA)

            byte[] pixels = new byte[height * stride];
            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
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

            byte[] pixels = BitmapSourceToByteArray(image, out int width, out int height);

            using var magickImage = new MagickImage(pixels, new MagickReadSettings
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = MagickFormat.Bgra,
                Depth = 8
            });
            magickImage.Quality = (uint)quality;

            using var target = new MemoryStream();
            magickImage.Write(target, MagickFormat.WebP);
            return target.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/webp";
    }

    public class AvifWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(ImageQueryParameterSet parameter)
        {
            int quality = parameter.Quality.Value;

            byte[] pixels = BitmapSourceToByteArray(image, out int width, out int height);

            using var magickImage = new MagickImage(pixels, new MagickReadSettings
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = MagickFormat.Bgra,
                Depth = 8
            });
            magickImage.Quality = (uint)quality;

            using var target = new MemoryStream();
            magickImage.Write(target, MagickFormat.Avif);
            return target.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/avif";
    }

    public class JxlWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(ImageQueryParameterSet parameter)
        {
            int quality = parameter.Quality.Value;

            byte[] pixels = BitmapSourceToByteArray(image, out int width, out int height);

            using var magickImage = new MagickImage(pixels, new MagickReadSettings
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = MagickFormat.Bgra,
                Depth = 8
            });
            magickImage.Quality = (uint)quality;

            using var target = new MemoryStream();
            magickImage.Write(target, MagickFormat.Jxl);
            return target.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/jxl";
    }
}
