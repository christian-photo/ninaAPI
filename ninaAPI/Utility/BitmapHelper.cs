#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
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
            string base64;

            if (size != Size.Empty) // Resize the image if requested
            {
                double scaling = size.Width / source.Width;

                source = new TransformedBitmap(source, new ScaleTransform(scaling, scaling));
            }

            if (quality < 0)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));

                base64 = EncoderToBase64(encoder);
            }
            else
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = quality;
                encoder.Frames.Add(BitmapFrame.Create(source));

                base64 = EncoderToBase64(encoder);
            }

            return base64;
        }

        public static string EncoderToBase64(JpegBitmapEncoder encoder)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        public static string EncoderToBase64(PngBitmapEncoder encoder)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                return Convert.ToBase64String(memory.ToArray());
            }
        }
    }
}
