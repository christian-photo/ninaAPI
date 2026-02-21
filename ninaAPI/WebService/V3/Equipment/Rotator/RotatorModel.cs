#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.DataAnnotations;

namespace ninaAPI.WebService.V3.Equipment.Rotator
{
    public class RotatorMoveConfig
    {
        [Required]
        [Range(0, float.MaxValue)]
        public float Position { get; set; }

        public bool MoveMechanical { get; set; }
    }

    public class RotatorSyncConfig
    {
        public float SkyAngle { get; set; }

        public bool SolveAndSync { get; set; }
    }
}
