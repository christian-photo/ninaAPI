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
using NINA.Sequencer.Container;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    /// <summary>
    /// All properties of this class are required
    /// </summary>
    public class TargetUpdate
    {
        public string TargetName { get; set; }

        public HttpCoordinates Coordinates { get; set; }

        [Range(0, 360)]
        public double? PositionAngle { get; set; }
    }

    public enum SequenceSkipType
    {
        SkipCurrentItems,
        SkipToImaging,
        SkipToEnd,
    }

    public class SequenceTarget
    {
        public string TargetName { get; set; }
        public double PositionAngle { get; set; }
        public Coordinates Coordinates { get; set; }
        public SequenceEntityStatus Status { get; set; }

        public SequenceTarget(IDeepSkyObjectContainer copyMe)
        {
            this.TargetName = copyMe.Target.TargetName;
            this.PositionAngle = copyMe.Target.PositionAngle;
            this.Coordinates = copyMe.Target.InputCoordinates.Coordinates;
            this.Status = copyMe.Status;
        }
    }

    public class SequenceEditBody
    {
        [Required(AllowEmptyStrings = false)]
        public string PathDescription { get; set; }

        [Required]
        public object Value { get; set; }
    }
}
