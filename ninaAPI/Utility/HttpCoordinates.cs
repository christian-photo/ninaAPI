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

namespace ninaAPI.Utility
{
    public class HttpCoordinates
    {
        [Range(-180, 180)]
        [Required]
        public double RA { get; set; }

        [Range(-90, 90)]
        [Required]
        public double Dec { get; set; }

        public Epoch? Epoch { get; set; }

        public Coordinates ToCoordinates()
        {
            return new Coordinates(Angle.ByDegree(RA), Angle.ByDegree(Dec), Epoch ?? NINA.Astrometry.Epoch.J2000);
        }
    }
}
