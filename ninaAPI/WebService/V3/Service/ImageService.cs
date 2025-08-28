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
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Service
{
    public static class ImageService
    {
        public static async Task<BitmapEncoder> ProcessAndPrepareImage(IRenderedImage image, ImageQueryParameterSet parameters)
        {
            image = await StretchAndDebayer(image, parameters);

            BitmapSource bitmap = ResizeBitmap(image.Image, parameters);
            BitmapEncoder encoder = BitmapHelper.GetEncoder(bitmap, parameters.Quality.Value);

            return encoder;
        }

        public static async Task<BitmapEncoder> ProcessAndPrepareImage(string path, bool isBayered, ImageQueryParameterSet parameters, int bitDepth = 16, int delay = 200, int retries = 10)
        {
            IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(path, bitDepth, isBayered, parameters.RawConverter.Value), TimeSpan.FromMilliseconds(delay), retries);
            IRenderedImage image = imageData.RenderImage();
            return await ProcessAndPrepareImage(image, parameters);
        }

        public static BitmapSource ResizeBitmap(BitmapSource image, ImageQueryParameterSet parameters)
        {
            if (parameters.Resize.Value)
            {
                if (parameters.Scale.WasProvided)
                {
                    image = BitmapHelper.ScaleBitmap(image, parameters.Scale.Value);
                }
                else
                {
                    image = BitmapHelper.ResizeBitmap(image, parameters.Size.Value);
                }
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
}