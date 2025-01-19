#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.IO;
using System;

namespace ninaAPI.Utility
{
    public static class BitmapHelper
    {
        public static string ResizeAndConvertBitmap(BitmapSource source, Size size, int quality)
        {
            return ScaleAndConvertBitmap(source, size == Size.Empty ? 1 : size.Width / source.Width, quality);
        }

        public static string ScaleAndConvertBitmap(BitmapSource source, double scale, int quality)
        {
            scale = Math.Clamp(scale, 0.1, 1);

            source = new TransformedBitmap(source, new ScaleTransform(scale, scale));

            string base64 = EncodeBitmap(source, quality);
            return base64;
        }

        private static string EncodeBitmap(BitmapSource source, int quality)
        {
            if (quality < 0)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));

                return EncoderToBase64(encoder);
            }
            else
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = quality;
                encoder.Frames.Add(BitmapFrame.Create(source));

                return EncoderToBase64(encoder);
            }
        }

        public static string EncoderToBase64(BitmapEncoder encoder)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                return Convert.ToBase64String(memory.ToArray());
            }
        }
    }
}
