#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using ninaAPI.Utility;
using NINA.Core.Utility;
using System.Threading.Tasks;
using System.Drawing;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Model;
using NINA.Image.Interfaces;
using NINA.Core.Enum;
using System.Collections.Generic;
using NINA.WPF.Base.Interfaces.Mediator;
using System.Windows.Media.Imaging;
using System.IO;

namespace ninaAPI.WebService.V2
{
    public class ImageWatcher : INinaWatcher
    {
        public static List<HttpResponse> Images = new List<HttpResponse>();

        public void StartWatchers()
        {
            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved += ImageSaved;
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved -= ImageSaved;
        }

        private static void ImageSaved(object sender, ImageSavedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "IMAGE-SAVE" },
                { "ImageStatistics", new Dictionary<string, object>() {
                    { "ExposureTime", e.Duration },
                    { "ImageType", e.MetaData.Image.ImageType },
                    { "Filter", e.Filter },
                    { "RmsText", e.MetaData.Image.RecordedRMS.TotalText },
                    { "Temperature", e.MetaData.Camera.Temperature },
                    { "CameraName", e.MetaData.Camera.Name },
                    { "Gain", e.MetaData.Camera.Gain },
                    { "Offset", e.MetaData.Camera.Offset },
                    { "Date", DateTime.Now },
                    { "TelescopeName", e.MetaData.Telescope.Name },
                    { "FocalLength", e.MetaData.Telescope.FocalLength },
                    { "StDev", e.Statistics.StDev },
                    { "Mean", e.Statistics.Mean },
                    { "Median", e.Statistics.Median },
                    { "Stars", e.StarDetectionAnalysis.DetectedStars },
                    { "HFR", e.StarDetectionAnalysis.HFR }
                    }
                }
            };

            HttpResponse imageEvent = new HttpResponse() { Type = HttpResponse.TypeSocket, Response = new Dictionary<string, object>() { { "Event", "IMAGE-SAVE" }, { "Time", DateTime.Now } } };

            Images.Add(response);
            WebSocketV2.Events.Add(imageEvent);

            WebSocketV2.SendEvent(response);
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/image/{index}")]
        public async Task GetImage(int index,
            [QueryField] bool resize,
            [QueryField] int quality,
            [QueryField] string size,
            [QueryField] double scale,
            [QueryField] double factor,
            [QueryField] double blackClipping,
            [QueryField] bool unlinked,
            [QueryField] bool stream,
            [QueryField] bool debayer,
            [QueryField] string bayerPattern)
        {
            HttpResponse response = new HttpResponse();
            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;

            SensorType sensor = SensorType.Monochrome;

            if (HttpContext.IsParameterOmitted(nameof(factor)))
            {
                factor = profile.ImageSettings.AutoStretchFactor;
            }
            if (HttpContext.IsParameterOmitted(nameof(blackClipping)))
            {
                blackClipping = profile.ImageSettings.BlackClipping;
            }
            if (HttpContext.IsParameterOmitted(nameof(unlinked)))
            {
                unlinked = profile.ImageSettings.UnlinkedStretch;
            }
            if (HttpContext.IsParameterOmitted(nameof(bayerPattern)))
            {
                sensor = AdvancedAPI.Controls.Camera.GetInfo().SensorType;
            }
            else
            {
                try
                {
                    sensor = Enum.Parse<SensorType>(bayerPattern);
                }
                catch (Exception)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Invalid bayer pattern", 400));
                    return;
                }
            }

            quality = Math.Clamp(quality, -1, 100);
            if (quality == 0)
                quality = -1; // quality should be set to -1 for png if omitted

            if (resize && string.IsNullOrWhiteSpace(size)) // workaround as default parameters are not working
                size = "640x480";

            try
            {
                Size sz = Size.Empty;
                if (resize)
                {
                    string[] s = size.Split('x');
                    int width = int.Parse(s[0]);
                    int height = int.Parse(s[1]);
                    sz = new Size(width, height);
                }

                IImageHistoryVM hist = AdvancedAPI.Controls.ImageHistory;
                if (hist.ImageHistory.Count <= 0)
                {
                    response = CoreUtility.CreateErrorTable(new Error("No images available", 500));
                }
                else if (index >= hist.ImageHistory.Count || index < 0)
                {
                    response = CoreUtility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE);
                }
                else
                {
                    ImageHistoryPoint p = hist.ImageHistory[index]; // Get the historyPoint at the specified index for the image

                    IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.LocalPath, 16, true, RawConverterEnum.FREEIMAGE), TimeSpan.FromMilliseconds(200), 10);
                    IRenderedImage renderedImage = imageData.RenderImage();

                    renderedImage = await renderedImage.Stretch(factor, blackClipping, unlinked);
                    if (debayer)
                    {
                        renderedImage = renderedImage.Debayer(bayerPattern: sensor);
                    }

                    if (stream)
                    {
                        BitmapEncoder encoder = null;
                        if (scale == 0 && resize)
                        {
                            BitmapSource image = BitmapHelper.ResizeBitmap(renderedImage.Image, sz);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        if (scale != 0 && resize)
                        {
                            BitmapSource image = BitmapHelper.ScaleBitmap(renderedImage.Image, scale);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        if (!resize)
                        {
                            BitmapSource image = BitmapHelper.ScaleBitmap(renderedImage.Image, 1);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        HttpContext.Response.ContentType = quality == -1 ? "image/png" : "image/jpg";
                        using (MemoryStream memory = new MemoryStream())
                        {
                            encoder.Save(memory);
                            await HttpContext.Response.OutputStream.WriteAsync(memory.ToArray());
                            return;
                        }
                    }
                    else
                    {

                        if (scale == 0 && resize)
                            response.Response = BitmapHelper.ResizeAndConvertBitmap(renderedImage.Image, sz, quality);
                        if (scale != 0 && resize)
                            response.Response = BitmapHelper.ScaleAndConvertBitmap(renderedImage.Image, scale, quality);
                        if (!resize)
                            response.Response = BitmapHelper.ScaleAndConvertBitmap(renderedImage.Image, 1, quality);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/image-history")]
        public void GetHistoryCount([QueryField] bool all, [QueryField] int index, [QueryField] bool count)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                List<object> result = new List<object>();
                if (count)
                {
                    response.Response = ImageWatcher.Images.Count;
                }
                else if (all)
                {
                    foreach (HttpResponse r in ImageWatcher.Images)
                    {
                        result.Add(((Dictionary<string, object>)r.Response)["ImageStatistics"]);
                    }
                    response.Response = result;
                }
                else if (index >= 0 && index < ImageWatcher.Images.Count)
                {
                    result.Add(((Dictionary<string, object>)ImageWatcher.Images[index].Response)["ImageStatistics"]);
                    response.Response = result;
                }
                else if (index >= ImageWatcher.Images.Count || index < 0)
                {
                    response = CoreUtility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
