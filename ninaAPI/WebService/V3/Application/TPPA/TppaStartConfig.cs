#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.ComponentModel.DataAnnotations;

namespace ninaAPI.WebService.V3.Application.TPPA
{
#nullable enable
    public class TppaStartConfig
    {
        private string action = string.Empty;

        [AllowedValues("StartAlignment", "StopAlignment", "PauseAlignment", "ResumeAlignment")]
        public string Action
        {
            get
            {
                if (action.Equals("StartAlignment") || action.Equals("StopAlignment"))
                {
                    return $"PolarAlignmentPlugin_DockablePolarAlignmentVM_{action}";
                }
                else
                {
                    return $"PolarAlignmentPlugin_PolarAlignment_{action}";
                }
            }
            set
            {
                action = value;
            }
        }
        public bool? ManualMode { get; set; }
        public int? TargetDistance { get; set; }
        public int? MoveRate { get; set; }
        public bool? EastDirection { get; set; }
        public bool? StartFromCurrentPosition { get; set; }

        [Range(0, 90)]
        public int? AltDegrees { get; set; }

        [Range(0, 60)]
        public int? AltMinutes { get; set; }

        [Range(0, 60)]
        public double? AltSeconds { get; set; }

        [Range(0, 360)]
        public int? AzDegrees { get; set; }

        [Range(0, 60)]
        public int? AzMinutes { get; set; }

        [Range(0, 60)]
        public double? AzSeconds { get; set; }

        [Range(0, 100)]
        public double? AlignmentTolerance { get; set; }
        public string? Filter { get; set; }

        [Range(0, double.MaxValue)]
        public double? ExposureTime { get; set; }

        [Range(1, short.MaxValue)]
        public short? Binning { get; set; }

        public int? Gain { get; set; }
        public int? Offset { get; set; }
        public double? SearchRadius { get; set; }

        // TODO: Check the ranges for gain, offset and search radius. Validate the others if possible
    }
}