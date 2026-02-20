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
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Websocket.TPPA
{
    public class TppaMessage(Guid correlatedGuid, string topic, object content) : IMessage
    {
        public Guid SenderId => Guid.Parse(AdvancedAPI.PluginId);

        public string Sender => nameof(ninaAPI);

        public DateTimeOffset SentAt => DateTime.UtcNow;

        public Guid MessageId => Guid.NewGuid();

        public DateTimeOffset? Expiration => null;

        public Guid? CorrelationId => correlatedGuid;

        public int Version => 1;

        public IDictionary<string, object> CustomHeaders => new Dictionary<string, object>();

        public string Topic => topic;

        public object Content => content;
    }

#nullable enable
    public class TppaConfig
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

        /// <summary>
        /// Validates the TPPA config based on its attributs
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the config is invalid</exception>
        public void Validate()
        {
            if (!action.Equals("StartAlignment") && !action.Equals("StopAlignment") && !action.Equals("PauseAlignment") && !action.Equals("ResumeAlignment"))
            {
                throw new ArgumentException("Action was not one of the supported actions, expected StartAlignment, StopAlignment, PauseAlignment or ResumeAlignment");
            }
            if (TargetDistance.HasValue && TargetDistance.Value < 0)
            {
                throw new ArgumentException("TargetDistance must be greater than 0");
            }
            if (MoveRate.HasValue && MoveRate.Value < 0)
            {
                throw new ArgumentException("MoveRate must be greater than 0");
            }
            if (AltDegrees.HasValue && !AltDegrees.Value.IsBetween(0, 90))
            {
                throw new ArgumentException("AltDegrees must be between 0 and 90");
            }
            if (AltMinutes.HasValue && !AltMinutes.Value.IsBetween(0, 60))
            {
                throw new ArgumentException("AltMinutes must be between 0 and 60");
            }
            if (AltSeconds.HasValue && !AltSeconds.Value.IsBetween(0, 60))
            {
                throw new ArgumentException("AltSeconds must be between 0 and 60");
            }
            if (AzDegrees.HasValue && !AzDegrees.Value.IsBetween(0, 360))
            {
                throw new ArgumentException("AzDegrees must be between 0 and 360");
            }
            if (AzMinutes.HasValue && !AzMinutes.Value.IsBetween(0, 60))
            {
                throw new ArgumentException("AzMinutes must be between 0 and 60");
            }
            if (AzSeconds.HasValue && !AzSeconds.Value.IsBetween(0, 60))
            {
                throw new ArgumentException("AzSeconds must be between 0 and 60");
            }
            if (AlignmentTolerance.HasValue && !AlignmentTolerance.Value.IsBetween(0, 100))
            {
                throw new ArgumentException("AlignmentTolerance must be between 0 and 100");
            }
            // if (Filter != null && !Filter.IsValidFilter())
            // {
            //     throw new ArgumentException("Filter is not a valid filter"); // TODO: Implement filter checking
            // }
            if (ExposureTime.HasValue && ExposureTime.Value < 0)
            {
                throw new ArgumentException("ExposureTime must be greater than 0");
            }
            if (Binning.HasValue && Binning.Value < 1)
            {
                throw new ArgumentException("Binning must be greater than 1");
            }
        }

        // TODO: Check the ranges for gain, offset and search radius. Validate the others if possible
    }
}