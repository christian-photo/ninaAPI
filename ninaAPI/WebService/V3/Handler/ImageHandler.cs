#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Handler
{
    public static class ImageHandler
    {
        public static async Task<BitmapEncoder> ProcessAndPrepareImage(IRenderedImage image, ImageQueryParameterSet parameters)
        {
            if (parameters.Debayer.Value)
            {
                image = image.Debayer(bayerPattern: parameters.BayerPattern.Value, saveColorChannels: true, saveLumChannel: true);
            }
            image = await image.Stretch(parameters.StretchFactor.Value, parameters.BlackClipping.Value, parameters.UnlinkedStretch.Value);

            BitmapEncoder encoder = null;
            BitmapSource bitmap = null;

            // TODO: Add ROI

            if (parameters.Resize.Value)
            {
                if (parameters.Scale.WasProvided)
                {
                    bitmap = BitmapHelper.ScaleBitmap(image.Image, parameters.Scale.Value);
                }
                else
                {
                    bitmap = BitmapHelper.ResizeBitmap(image.Image, parameters.Size.Value);
                }
            }
            else
            {
                bitmap = BitmapHelper.ScaleBitmap(image.Image, 1);
            }

            encoder = BitmapHelper.GetEncoder(bitmap, parameters.Quality.Value);

            return encoder;
        }

        public static async Task<BitmapEncoder> PrepareImage(string path, bool isBayered, ImageQueryParameterSet parameters, int bitDepth = 16, int delay = 200, int retries = 10)
        {
            IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(path, bitDepth, isBayered, parameters.RawConverter.Value), TimeSpan.FromMilliseconds(delay), retries);
            IRenderedImage image = imageData.RenderImage();
            return await ProcessAndPrepareImage(image, parameters);
        }
    }
}