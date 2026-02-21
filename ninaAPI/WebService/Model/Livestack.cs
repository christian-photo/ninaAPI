#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using NINA.Plugin.Interfaces;

namespace ninaAPI.WebService.Model
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
        }

        public BitmapSource GetLast(string filter, string target)
        {
            return Images.LastOrDefault(x => x.Filter == filter && x.Target == target)?.Image;
        }
    }
}