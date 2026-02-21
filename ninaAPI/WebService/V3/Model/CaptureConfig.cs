#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.ComponentModel.DataAnnotations;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile.Interfaces;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Model
{
    public class CaptureConfig
    {
        [Range(0, double.MaxValue)]
        public double? Duration { get; set; }

        [Range(0, int.MaxValue)]
        public int? Gain { get; set; }

        [Range(0, 1)]
        public double? ROI { get; set; } = 1;
        public BinningMode Binning { get; set; }

        public string ImageType { get; set; }

        public string TargetName { get; set; }

        public void UpdateDefaults(IPlateSolveSettings solveSettings, CameraInfo info)
        {
            Duration ??= solveSettings.ExposureTime;
            Gain ??= info.Gain;
            ROI ??= 1;
            Binning ??= new BinningMode(info.BinX, info.BinY);
            TargetName = string.IsNullOrEmpty(TargetName) ? "Snapshot" : TargetName;
            ImageType = ensureValidImageType();
        }

        private string ensureValidImageType(string defaultType = "SNAPSHOT")
        {
            if (string.IsNullOrEmpty(ImageType)) return defaultType;
            ImageType = ImageType.ToUpper();
            if (CoreUtility.IMAGE_TYPES.Contains(ImageType))
            {
                return ImageType;
            }
            return defaultType;
        }
    }
}
