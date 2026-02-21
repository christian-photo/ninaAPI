#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.DataAnnotations;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    /// <summary>
    /// All properties of this class are required
    /// </summary>
    public class TargetUpdate
    {
        [Required]
        public string TargetName { get; set; }

        [Required]
        public HttpCoordinates Coordinates { get; set; }

        [Required]
        [Range(0, 360)]
        public double Rotation { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int TargetIndex { get; set; }
    }
}
