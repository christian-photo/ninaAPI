#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using NINA.WPF.Base.Interfaces.Mediator;

namespace ninaAPI.WebService.V3.Model
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
        public int BitDepth { get; set; }
        public string Filename { get => Path?.IsFile == true ? System.IO.Path.GetFileName(Path.LocalPath) : null; }

        private Uri Path { get; set; }
        private Uri ThumbnailPath { get; set; }

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
                BitDepth = e.Statistics.BitDepth,
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

        public string GetThumbnailPath()
        {
            return ThumbnailPath?.LocalPath;
        }

        public void SetThumbnailPath(string path)
        {
            ThumbnailPath = new Uri(path);
        }
    }
}
