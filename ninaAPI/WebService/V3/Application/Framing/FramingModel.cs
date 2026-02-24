#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.ComponentModel.DataAnnotations;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Application.Framing
{
    public class FramingInfoContainer(IFramingAssistantVM framing)
    {
        public double BoundHeight { get; set; } = framing.BoundHeight;
        public double BoundWidth { get; set; } = framing.BoundWidth;
        public int CameraHeight { get; set; } = framing.CameraHeight;
        public int CameraWidth { get; set; } = framing.CameraWidth;
        public double CameraPixelSize { get; set; } = framing.CameraPixelSize;
        public Coordinates Coordinates { get; set; } = framing.DSO.Coordinates;
        public string DSOName { get; set; } = framing.DSO.Name;
        public double FieldOfView { get; set; } = framing.FieldOfView;
        public double FocalLength { get; set; } = framing.FocalLength;
        public int HorizontalPanels { get; set; } = framing.HorizontalPanels;
        public int VerticalPanels { get; set; } = framing.VerticalPanels;
        public SkySurveySource FramingSource { get; set; } = framing.FramingAssistantSource;
    }

    public class FramingUpdate
    {
        [Range(0, double.MaxValue)]
        public double? BoundHeight { get; set; }

        [Range(0, double.MaxValue)]
        public double? BoundWidth { get; set; }

        [Range(0, int.MaxValue)]
        public int? CameraHeight { get; set; }

        [Range(0, int.MaxValue)]
        public int? CameraWidth { get; set; }

        [Range(0, double.MaxValue)]
        public double? CameraPixelSize { get; set; }

        public HttpCoordinates Coordinates { get; set; }

        public string DSOName { get; set; }

        [Range(0, double.MaxValue)]
        public double? FieldOfView { get; set; }

        [Range(0, double.MaxValue)]
        public double? FocalLength { get; set; }

        [Range(0, int.MaxValue)]
        public int? HorizontalPanels { get; set; }

        [Range(0, int.MaxValue)]
        public int? VerticalPanels { get; set; }

        public SkySurveySource? FramingSource { get; set; }
    }
}