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
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Service
{
    public abstract class ImageWriter(BitmapSource img)
    {
        public static ImageWriter GetImageWriter(BitmapSource image, ImageFormat format)
        {
            return format switch
            {
                // ImageFormat.AVIF => new AvifWriter(image),
                // ImageFormat.JXL => new JxlWriter(image),
                ImageFormat.JPEG => new JpegWriter(image),
                ImageFormat.PNG => new PngWriter(image),
                // ImageFormat.WEBP => new WebpWriter(image),
                _ => throw new NotImplementedException(),
            };
        }


        protected BitmapSource image = img;

        public abstract byte[] Encode(int quality);
        public abstract string MimeType { get; protected set; }

        // protected static Image BitmapSourceToVipsImage(BitmapSource bitmapSource)
        // {
        //     if (bitmapSource.Format != PixelFormats.Rgb24)
        //     {
        //         var converted = new FormatConvertedBitmap();
        //         converted.BeginInit();
        //         converted.Source = bitmapSource;
        //         converted.DestinationFormat = PixelFormats.Rgb24;
        //         converted.EndInit();
        //         bitmapSource = converted;
        //     }

        //     int width = bitmapSource.PixelWidth;
        //     int height = bitmapSource.PixelHeight;
        //     int stride = width * 3; // 3 bytes per pixel, no alpha
        //     byte[] pixels = new byte[height * stride];
        //     bitmapSource.CopyPixels(pixels, stride, 0);

        //     var vipsImage = Image.NewFromMemory(
        //         pixels,
        //         width,
        //         height,
        //         bands: 3,
        //         format: Enums.BandFormat.Uchar
        //     );

        //     return vipsImage.Copy(interpretation: Enums.Interpretation.Srgb);
        // }
    }

    public class JpegWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(int quality)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = quality;
            encoder.Frames.Add(BitmapFrame.Create(image));

            using var target = new MemoryStream();
            encoder.Save(target);
            return target.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/jpeg";
    }

    public class PngWriter(BitmapSource image) : ImageWriter(image)
    {
        public override byte[] Encode(int quality)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        public override string MimeType { get; protected set; } = "image/png";
    }

    // public class WebpWriter(BitmapSource image) : ImageWriter(image)
    // {
    //     public override byte[] Encode(int quality)
    //     {
    //         using var vipsImage = BitmapSourceToVipsImage(image);

    //         // WebP with additional options
    //         return vipsImage.WebpsaveBuffer(
    //             q: quality,
    //             lossless: quality >= 100,  // Use lossless for quality 100
    //             nearLossless: quality >= 95,  // Near-lossless for very high quality
    //             effort: 4  // 0-6, higher = better compression but slower
    //         );
    //     }

    //     public override string MimeType { get; protected set; } = "image/webp";
    // }

    // public class AvifWriter(BitmapSource image) : ImageWriter(image)
    // {
    //     public override byte[] Encode(int quality)
    //     {
    //         using var vipsImage = BitmapSourceToVipsImage(image);

    //         // AVIF with additional options
    //         return vipsImage.HeifsaveBuffer(
    //             q: quality,
    //             lossless: quality >= 100,
    //             compression: Enums.ForeignHeifCompression.Av1, // Why is this sooo slow??
    //             effort: 4  // encoding effort
    //         );
    //     }

    //     public override string MimeType { get; protected set; } = "image/avif";
    // }

    // public class JxlWriter(BitmapSource image) : ImageWriter(image)
    // {
    //     public override byte[] Encode(int quality)
    //     {
    //         using var vipsImage = BitmapSourceToVipsImage(image);

    //         // AVIF with additional options
    //         return vipsImage.JxlsaveBuffer(
    //             q: quality,
    //             lossless: quality >= 100,
    //             effort: 4  // encoding effort
    //         );
    //     }

    //     public override string MimeType { get; protected set; } = "image/jxl";
    // }
}
