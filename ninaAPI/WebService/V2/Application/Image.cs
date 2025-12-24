#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.FileFormat.FITS;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V2
{
    public class ImageResponse
    {
        public double ExposureTime { get; set; }
        public string ImageType { get; set; }
        public string Filter { get; set; }
        public string RmsText { get; set; }
        public double Temperature { get; set; }
        public string CameraName { get; set; }
        public string TargetName { get; set; }
        public int Gain { get; set; }
        public int Offset { get; set; }
        public DateTime Date { get; set; }
        public string TelescopeName { get; set; }
        public double FocalLength { get; set; }
        public double StDev { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int Stars { get; set; }
        public double HFR { get; set; }
        public double HFRStDev { get; set; }
        public bool IsBayered { get; set; }
        public string Filename { get => Path?.IsFile == true ? System.IO.Path.GetFileName(Path.LocalPath) : null; }

        private Uri Path { get; set; }

        private ImageResponse() { }

        public static ImageResponse FromEvent(ImageSavedEventArgs e)
        {
            return new ImageResponse()
            {
                ExposureTime = e.Duration,
                TargetName = e.MetaData.Target.Name,
                ImageType = e.MetaData.Image.ImageType,
                Filter = e.Filter,
                RmsText = e.MetaData.Image.RecordedRMS.TotalText,
                Temperature = e.MetaData.Camera.Temperature,
                CameraName = e.MetaData.Camera.Name,
                Gain = e.MetaData.Camera.Gain,
                Offset = e.MetaData.Camera.Offset,
                Date = DateTime.Now,
                TelescopeName = e.MetaData.Telescope.Name,
                FocalLength = e.MetaData.Telescope.FocalLength,
                StDev = e.Statistics.StDev,
                Mean = e.Statistics.Mean,
                Median = e.Statistics.Median,
                Stars = e.StarDetectionAnalysis.DetectedStars,
                HFR = e.StarDetectionAnalysis.HFR,
                HFRStDev = e.StarDetectionAnalysis.HFRStDev,
                Min = e.Statistics.Min,
                Max = e.Statistics.Max,
                Path = e.PathToImage,
                IsBayered = e.IsBayered,
            };
        }

        public string GetPath()
        {
            return Path.LocalPath;
        }

        public void SetPath(string path)
        {
            Path = new Uri(path);
        }
    }

    public class ImageEvent(ImageResponse stats)
    {
        public string Event { get; set; } = "IMAGE-SAVE";
        public ImageResponse ImageStatistics { get; set; } = stats;
    }

    public class ImageWatcher : INinaWatcher
    {
        public static List<ImageResponse> Images = new List<ImageResponse>();
        public static List<KeyValuePair<int, string>> Thumbnails = new List<KeyValuePair<int, string>>();
        public static IRenderedImage PreparedImage { get; private set; }

        public static object imageLock = new object();

        public void StartWatchers()
        {
            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved += ImageSaved;
            AdvancedAPI.Controls.Imaging.ImagePrepared += ImagePrepared;
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved -= ImageSaved;
            AdvancedAPI.Controls.Imaging.ImagePrepared -= ImagePrepared;
        }

        private static async void ImagePrepared(object sender, ImagePreparedEventArgs e)
        {
            lock (imageLock)
            {
                PreparedImage = e.RenderedImage;
            }

            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };

            response.Response = new Dictionary<string, object>() { { "Event", "IMAGE-PREPARED" } };

            await WebSocketV2.SendEvent(response);
        }

        private static void CacheThumbnail(ImageSavedEventArgs e)
        {
            if (!Properties.Settings.Default.CreateThumbnails)
                return;
            lock (imageLock)
            {
                string thumbnailFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"thumbnails-{Environment.ProcessId}");
                Directory.CreateDirectory(thumbnailFile);
                thumbnailFile = Path.Combine(thumbnailFile, $"{Images.Count - 1}.jpg");
                var img = BitmapHelper.ScaleBitmap(e.Image, 256 / e.Image.Width);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 100;
                encoder.Frames.Add(BitmapFrame.Create(img));

                using (FileStream fs = new FileStream(thumbnailFile, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                Thumbnails.Add(new KeyValuePair<int, string>(Thumbnails.Count, thumbnailFile));
            }
        }

        private static async void ImageSaved(object sender, ImageSavedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };

            var r = ImageResponse.FromEvent(e);

            response.Response = new ImageEvent(r);

            HttpResponse imageEvent = new HttpResponse() { Type = HttpResponse.TypeSocket, Response = new Dictionary<string, object>() { { "Event", "IMAGE-SAVE" }, { "Time", DateTime.Now } } };

            lock (imageLock)
            {
                Images.Add(r);
            }

            WebSocketV2.Events.Add(imageEvent);

            CacheThumbnail(e);

            await WebSocketV2.SendEvent(response);
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/prepared-image")]
        public async Task GetPreparedImage([QueryField] bool resize,
                    [QueryField] int quality,
                    [QueryField] string size,
                    [QueryField] double scale,
                    [QueryField] double factor,
                    [QueryField] double blackClipping,
                    [QueryField] bool unlinked,
                    [QueryField] bool debayer,
                    [QueryField] bool autoPrepare,
                    [QueryField] string bayerPattern)
        {
            HttpResponse response = new HttpResponse();
            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;

            SensorType sensor = SensorType.Monochrome;

            if (HttpContext.IsParameterOmitted(nameof(factor)) || autoPrepare)
            {
                factor = profile.ImageSettings.AutoStretchFactor;
            }
            if (HttpContext.IsParameterOmitted(nameof(blackClipping)) || autoPrepare)
            {
                blackClipping = profile.ImageSettings.BlackClipping;
            }
            if (HttpContext.IsParameterOmitted(nameof(unlinked)) || autoPrepare)
            {
                unlinked = profile.ImageSettings.UnlinkedStretch;
            }
            if (HttpContext.IsParameterOmitted(nameof(bayerPattern)) || autoPrepare)
            {
                if (profile.CameraSettings.BayerPattern != BayerPatternEnum.Auto)
                {
                    sensor = (SensorType)profile.CameraSettings.BayerPattern;
                }
                else if (AdvancedAPI.Controls.Camera.GetInfo().Connected)
                {
                    sensor = AdvancedAPI.Controls.Camera.GetInfo().SensorType;
                }
                else
                {
                    sensor = SensorType.Monochrome;
                }
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
                    HttpContext.WriteToResponse(response);
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

                IRenderedImage renderedImage = ImageWatcher.PreparedImage;

                if (renderedImage is null)
                {
                    response = CoreUtility.CreateErrorTable(new Error("No image", 404));
                    HttpContext.WriteToResponse(response);
                    return;
                }

                if (debayer || (autoPrepare && renderedImage.RawImageData.Properties.IsBayered))
                {
                    try
                    {
                        renderedImage = renderedImage.Debayer(bayerPattern: sensor, saveColorChannels: true, saveLumChannel: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
                renderedImage = await renderedImage.Stretch(factor, blackClipping, unlinked);

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
                HttpContext.Response.ContentType = quality == -1 ? "image/png" : "image/jpeg";
                using (MemoryStream memory = new MemoryStream())
                {
                    encoder.Save(memory);
                    await HttpContext.Response.OutputStream.WriteAsync(memory.ToArray());
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

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
                    [QueryField] string bayerPattern,
                    [QueryField] bool autoPrepare,
                    [QueryField] string imageType,
                    [QueryField] bool raw_fits)
        {
            HttpResponse response = new HttpResponse();
            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;

            SensorType sensor = SensorType.Monochrome;

            if (HttpContext.IsParameterOmitted(nameof(factor)) || autoPrepare)
            {
                factor = profile.ImageSettings.AutoStretchFactor;
            }
            if (HttpContext.IsParameterOmitted(nameof(blackClipping)) || autoPrepare)
            {
                blackClipping = profile.ImageSettings.BlackClipping;
            }
            if (HttpContext.IsParameterOmitted(nameof(unlinked)) || autoPrepare)
            {
                unlinked = profile.ImageSettings.UnlinkedStretch;
            }
            if (HttpContext.IsParameterOmitted(nameof(bayerPattern)) || autoPrepare)
            {
                if (profile.CameraSettings.BayerPattern != BayerPatternEnum.Auto)
                {
                    sensor = (SensorType)profile.CameraSettings.BayerPattern;
                }
                else if (AdvancedAPI.Controls.Camera.GetInfo().Connected)
                {
                    sensor = AdvancedAPI.Controls.Camera.GetInfo().SensorType;
                }
                else
                {
                    sensor = SensorType.Monochrome;
                }
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
                    HttpContext.WriteToResponse(response);
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

                IEnumerable<ImageResponse> points;
                lock (ImageWatcher.imageLock)
                {
                    points = HttpContext.IsParameterOmitted(nameof(imageType)) ? ImageWatcher.Images : ImageWatcher.Images.Where(x => x.ImageType.Equals(imageType));
                }

                if (!points.Any())
                {
                    response = CoreUtility.CreateErrorTable(new Error("No images available", 500));
                }
                else if (index >= points.Count() || index < 0)
                {
                    response = CoreUtility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE);
                }
                else
                {
                    ImageResponse p = points.ElementAt(index); // Get the history point at the specified index for the image

                    IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.GetPath(), 16, p.IsBayered, RawConverterEnum.FREEIMAGE), TimeSpan.FromMilliseconds(200), 10);

                    if (HttpContext.IsParameterOmitted(nameof(raw_fits)))
                    {
                        IRenderedImage renderedImage = imageData.RenderImage();

                        if (debayer || (autoPrepare && renderedImage.RawImageData.Properties.IsBayered))
                        {
                            renderedImage = renderedImage.Debayer(bayerPattern: sensor, saveColorChannels: true, saveLumChannel: true);
                        }
                        renderedImage = await renderedImage.Stretch(factor, blackClipping, unlinked);


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
                            HttpContext.Response.ContentType = quality == -1 ? "image/png" : "image/jpeg";
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
                    else
                    {
                        // If the image is not of type FITS return an error.
                        if (!p.GetPath().EndsWith(".fits", true, null))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Image is not a FITS file", 400));
                        }
                        else
                        {
                            // Create the FITS image.
                            FITS f = new FITS(
                                imageData.Data.FlatArray,
                                imageData.Properties.Width,
                                imageData.Properties.Height
                            );

                            f.PopulateHeaderCards(imageData.MetaData);

                            // Populate the stream with the raw byte data of the FITS image.
                            MemoryStream fits_stream = new MemoryStream();

                            f.Write(fits_stream);

                            // Encode the stream in base64 and send it.
                            response.Response = Convert.ToBase64String(fits_stream.ToArray());
                        }
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

        [Route(HttpVerbs.Get, "/prepared-image/solve")]
        public async Task SolvePreparedImage()
        {
            if (ImageWatcher.PreparedImage is null)
            {
                HttpContext.WriteToResponse(CoreUtility.CreateErrorTable(new Error("Image not available", 404)));
                return;
            }
            await SolveImage(-1, string.Empty, ImageWatcher.PreparedImage);
        }

        [Route(HttpVerbs.Get, "/image/{index}/solve")]
        public async Task SolveImage(int index, [QueryField] string imageType, object image = null)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IRenderedImage img = null;
                if (image is null)
                {
                    IEnumerable<ImageResponse> points;
                    lock (ImageWatcher.imageLock)
                    {
                        points = HttpContext.IsParameterOmitted(nameof(imageType)) ? ImageWatcher.Images : ImageWatcher.Images.Where(x => x.ImageType.Equals(imageType));
                    }

                    if (!points.Any())
                    {
                        response = CoreUtility.CreateErrorTable(new Error("No images available", 400));
                        HttpContext.WriteToResponse(response);
                        return;
                    }
                    else if (index >= points.Count() || index < 0)
                    {
                        response = CoreUtility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE);
                        HttpContext.WriteToResponse(response);
                        return;
                    }
                    else
                    {
                        ImageResponse p = points.ElementAt(index);
                        IImageData imageData = await Retry.Do(async () => await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.GetPath(), 16, p.IsBayered, RawConverterEnum.FREEIMAGE), TimeSpan.FromMilliseconds(200), 10);
                        img = imageData.RenderImage();
                    }
                }
                else
                {
                    img = (IRenderedImage)image;
                }

                plateSolveResult = null;
                IPlateSolveSettings settings = AdvancedAPI.Controls.Profile.ActiveProfile.PlateSolveSettings;

                IPlateSolverFactory platesolver = AdvancedAPI.Controls.PlateSolver;
                Coordinates coordinates = AdvancedAPI.Controls.Mount.GetCurrentPosition();
                double focalLength = AdvancedAPI.Controls.Profile.ActiveProfile.TelescopeSettings.FocalLength;
                CaptureSolverParameter solverParameter = new CaptureSolverParameter()
                {
                    Attempts = 1,
                    Binning = settings.Binning,
                    BlindFailoverEnabled = settings.BlindFailoverEnabled,
                    Coordinates = coordinates,
                    DownSampleFactor = settings.DownSampleFactor,
                    FocalLength = focalLength,
                    MaxObjects = settings.MaxObjects,
                    Regions = settings.Regions,
                    SearchRadius = settings.SearchRadius,
                    PixelSize = img.RawImageData.MetaData.Camera.PixelSize
                };
                IImageSolver captureSolver = platesolver.GetImageSolver(platesolver.GetPlateSolver(settings), platesolver.GetBlindSolver(settings));

                plateSolveResult = await captureSolver.Solve(img.RawImageData, solverParameter, AdvancedAPI.Controls.StatusMediator.GetStatus(), HttpContext.CancellationToken);

                response.Response = plateSolveResult;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);

        }

        [Route(HttpVerbs.Get, "/image/{index}/prefix")]
        public void AddPrefix(int index, [QueryField(true)] string prefix, [QueryField] string imageType)
        {
            HttpResponse response = new HttpResponse();

            IEnumerable<ImageResponse> points;
            lock (ImageWatcher.imageLock)
            {
                points = HttpContext.IsParameterOmitted(nameof(imageType)) ? ImageWatcher.Images : ImageWatcher.Images.Where(x => x.ImageType.Equals(imageType));
            }

            if (!points.Any())
            {
                response = CoreUtility.CreateErrorTable(new Error("No images available", 400));
            }
            else if (index >= points.Count() || index < 0)
            {
                response = CoreUtility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE);
            }
            else
            {
                ImageResponse p = points.ElementAt(index);
                string newPath = Path.Join(Path.GetDirectoryName(p.GetPath()), prefix + Path.GetFileName(p.GetPath()));
                if (File.Exists(newPath))
                {
                    response = CoreUtility.CreateErrorTable(new Error("File already exists", 400));
                }
                else if (!File.Exists(p.GetPath()))
                {
                    response = CoreUtility.CreateErrorTable(new Error("Image file does not exist", 400));
                }
                else
                {
                    File.Move(p.GetPath(), newPath);
                    p.SetPath(newPath);
                    response.Response = "Image renamed with prefix " + prefix;
                }
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/image-history")]
        public void GetHistoryCount([QueryField] bool all, [QueryField] int index, [QueryField] bool count, [QueryField] string imageType)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                var images = HttpContext.IsParameterOmitted(nameof(imageType)) ? ImageWatcher.Images : ImageWatcher.Images.Where(x => x.ImageType.Equals(imageType));
                index = HttpContext.IsParameterOmitted(nameof(index)) ? images.Count() - 1 : index;
                if (count)
                {
                    response.Response = images.Count();
                }
                else if (all)
                {
                    response.Response = images;
                }
                else if (index >= 0 && index < images.Count())
                {
                    List<object> result = [images.ElementAt(index)]; // TODO: put this directly into the response (maybe)
                    response.Response = result;
                }
                else if (index >= images.Count() || index < 0)
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

        [Route(HttpVerbs.Get, "/image/thumbnail/{index}")]
        public async Task GetImage(int index,
                    [QueryField] string imageType)
        {
            HttpResponse response = new HttpResponse();
            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;

            try
            {
                if (ImageWatcher.Thumbnails.Count == 0)
                {
                    response = CoreUtility.CreateErrorTable(new Error("No thumbnails available", 400));
                }
                else
                {
                    string res;
                    lock (ImageWatcher.imageLock)
                    {
                        var images = HttpContext.IsParameterOmitted(nameof(imageType)) ? ImageWatcher.Images : ImageWatcher.Images.Where(x => x.ImageType.Equals(imageType));

                        var i = ImageWatcher.Images.IndexOf(images.ElementAt(index));
                        res = ImageWatcher.Thumbnails.Where(x => x.Key == i).First().Value;
                        HttpContext.Response.ContentType = "image/jpeg";
                    }

                    using (FileStream fs = File.OpenRead(res))
                    {
                        await fs.CopyToAsync(HttpContext.Response.OutputStream);
                        return;
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
    }
}
