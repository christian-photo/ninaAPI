#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.DataAnnotations;
using NINA.Astrometry;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Equipment.Mount
{
    public class MountSlewConfig
    {
        [Required]
        public HttpCoordinates Coordinates { get; set; }

        [Range(0, 360)]
        public double? PositionAngle { get; set; }

        [Required]
        public SlewType SlewType { get; set; }
    }

    public class MountSyncConfig
    {
        public HttpCoordinates Coordinates { get; set; }

        public bool SolveAndSync { get; set; }
    }

    public class MountFlipConfig
    {
        public bool Recenter { get; set; }
        public bool AutofocusAfterFlip { get; set; }
    }

    public enum SlewType
    {
        Slew,
        Center,
        Rotate
    }
}
