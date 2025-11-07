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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V2
{
    public class LiveStackResponse(bool isMonochrome, int? stackCount, int? redImages, int? greenImages, int? blueImages, string filter, string target, BitmapSource image)
    {
        public bool IsMonochrome { get; set; } = isMonochrome;
        public int? StackCount { get; set; } = stackCount;
        public int? RedStackCount { get; set; } = redImages;
        public int? GreenStackCount { get; set; } = greenImages;
        public int? BlueStackCount { get; set; } = blueImages;
        public string Filter { get; set; } = filter;
        public string Target { get; set; } = target;
        public BitmapSource Image { get; set; } = image;
    }

    public class LiveStackHistory : IDisposable
    {
        public List<LiveStackResponse> Images { get; set; } = new List<LiveStackResponse>();

        public void AddMono(string filter, int stackCount, string target, BitmapSource image)
        {
            Add(new LiveStackResponse(true, stackCount, null, null, null, filter, target, image));
        }

        public void AddColor(int redImages, int greenImages, int blueImages, string filter, string target, BitmapSource image)
        {
            Add(new LiveStackResponse(false, null, redImages, greenImages, blueImages, filter, target, image));
        }

        public void Add(LiveStackResponse image)
        {
            Images.RemoveAll(x => x.Filter == image.Filter && x.Target == image.Target); // Only keep the last stacked image for each filter and target
            Images.Add(image);
        }

        public void Dispose()
        {
            Images.Clear();
            Images = null;
        }

        public BitmapSource GetLast(string filter, string target)
        {
            return Images.LastOrDefault(x => x.Filter == filter && x.Target == target).Image;
        }
    }

    public class LiveStackWatcher : INinaWatcher, ISubscriber
    {
        public static LiveStackHistory LiveStackHistory { get; private set; }

        public async Task OnMessageReceived(IMessage message)
        {
            string filter = message.Content.GetType().GetProperty("Filter").GetValue(message.Content).ToString();
            string target = message.Content.GetType().GetProperty("Target").GetValue(message.Content).ToString();
            bool isMono = (bool)message.Content.GetType().GetProperty("IsMonochrome").GetValue(message.Content);
            int? stackCount = (int?)message.Content.GetType().GetProperty("StackCount").GetValue(message.Content);
            int? redImages = (int?)message.Content.GetType().GetProperty("RedStackCount").GetValue(message.Content);
            int? greenImages = (int?)message.Content.GetType().GetProperty("GreenStackCount").GetValue(message.Content);
            int? blueImages = (int?)message.Content.GetType().GetProperty("BlueStackCount").GetValue(message.Content);

            if (message.Content.GetType().GetProperty("Image").GetValue(message.Content) is BitmapSource image)
            {
                if (isMono)
                {
                    LiveStackHistory.AddMono(filter, stackCount ?? -1, target, image);
                    await WebSocketV2.SendAndAddEvent("STACK-UPDATED", new Dictionary<string, object>()
                    {
                        { "Filter", filter },
                        { "Target", target },
                        { "IsMonochrome", isMono },
                        { "StackCount", stackCount },
                    });
                }
                else
                {
                    LiveStackHistory.AddColor(redImages ?? -1, greenImages ?? -1, blueImages ?? -1, filter, target, image);
                    await WebSocketV2.SendAndAddEvent("STACK-UPDATED", new Dictionary<string, object>()
                    {
                        { "Filter", filter },
                        { "Target", target },
                        { "IsMonochrome", isMono },
                        { "GreenStackCount", greenImages },
                        { "RedStackCount", redImages },
                        { "BlueStackCount", blueImages },
                    });
                }
            }
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.MessageBroker.Subscribe("Livestack_LivestackDockable_StackUpdateBroadcast", this);
            LiveStackHistory = new LiveStackHistory();
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.MessageBroker.Unsubscribe("Livestack_LivestackDockable_StackUpdateBroadcast", this);
            LiveStackHistory.Dispose();
        }
    }

    public class LiveStackMessage(Guid correlatedGuid, string topic, object content) : IMessage
    {
        public Guid SenderId => Guid.Parse(AdvancedAPI.PluginId);

        public string Sender => nameof(ninaAPI);

        public DateTimeOffset SentAt => DateTimeOffset.Now;

        public Guid MessageId => Guid.NewGuid();

        public DateTimeOffset? Expiration => null;

        public Guid? CorrelationId => correlatedGuid;

        public int Version => 1;

        public IDictionary<string, object> CustomHeaders => new Dictionary<string, object>();

        public string Topic => topic;

        public object Content => content;
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/livestack/stop")]
        public void LiveStackStop()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                AdvancedAPI.Controls.MessageBroker.Publish(new LiveStackMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StopLiveStack", string.Empty));
                response.Response = "Live stack stopped";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/livestack/start")]
        public void LiveStackStart()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                AdvancedAPI.Controls.MessageBroker.Publish(new LiveStackMessage(Guid.NewGuid(), "Livestack_LivestackDockable_StartLiveStack", string.Empty));
                response.Response = "Live stack started";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/livestack/image/available")]
        public void LiveStackImageAvailable()
        {
            HttpResponse response = new HttpResponse();

            List<object> images = new List<object>();

            try
            {
                foreach (var image in LiveStackWatcher.LiveStackHistory.Images)
                {
                    images.Add(new { image.Filter, image.Target });
                }
                response.Response = images;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/livestack/image/{target}/{filter}")]
        public async Task LiveStackImage(string filter, string target,
            [QueryField] bool resize,
            [QueryField] int quality,
            [QueryField] string size,
            [QueryField] double scale,
            [QueryField] bool stream)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                BitmapSource image = LiveStackWatcher.LiveStackHistory.GetLast(filter, target);
                if (image is null)
                {
                    response = CoreUtility.CreateErrorTable(new Error("No image with specified filter and target found", 404));
                }
                else
                {
                    quality = Math.Clamp(quality, -1, 100);
                    if (quality == 0)
                        quality = -1; // quality should be set to -1 for png if omitted

                    if (resize && string.IsNullOrWhiteSpace(size)) // workaround as default parameters are not working
                        size = "640x480";

                    Size sz = Size.Empty;
                    if (resize)
                    {
                        string[] s = size.Split('x');
                        int width = int.Parse(s[0]);
                        int height = int.Parse(s[1]);
                        sz = new Size(width, height);
                    }
                    if (stream)
                    {
                        BitmapEncoder encoder = null;
                        if (scale == 0 && resize)
                        {
                            image = BitmapHelper.ResizeBitmap(image, sz);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        if (scale != 0 && resize)
                        {
                            image = BitmapHelper.ScaleBitmap(image, scale);
                            encoder = BitmapHelper.GetEncoder(image, quality);
                        }
                        if (!resize)
                        {
                            image = BitmapHelper.ScaleBitmap(image, 1);
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
                            response.Response = BitmapHelper.ResizeAndConvertBitmap(image, sz, quality);
                        if (scale != 0 && resize)
                            response.Response = BitmapHelper.ScaleAndConvertBitmap(image, scale, quality);
                        if (!resize)
                            response.Response = BitmapHelper.ScaleAndConvertBitmap(image, 1, quality);
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

        [Route(HttpVerbs.Get, "/livestack/image/{target}/{filter}/info")]
        public async Task LiveStackImageInfo(string filter, string target)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                LiveStackResponse l = LiveStackWatcher.LiveStackHistory.Images.Where(x => x.Filter == filter && x.Target == target).LastOrDefault();
                if (l is null)
                {
                    response = CoreUtility.CreateErrorTable(new Error("No image with specified filter and target found", 404));
                }
                else
                {

                    response.Response = new
                    {
                        IsMonochrome = l.IsMonochrome,
                        StackCount = l.StackCount,
                        RedStackCount = l.RedStackCount,
                        GreenStackCount = l.GreenStackCount,
                        BlueStackCount = l.BlueStackCount,
                        Filter = l.Filter,
                        Target = l.Target,
                    };

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