#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using NINA.Astrometry;

namespace ninaAPI.Utility
{
    public class MoonSunSeparation
    {
        public double Separation { get; set; }

        public MoonSunSeparation(DateTime date, bool toMoon, ObserverInfo observerInfo, Coordinates targetCoordinates)
        {
            var sepObjectPosition = new NOVAS.SkyPosition();

            if (toMoon)
            {
                sepObjectPosition = AstroUtil.GetMoonPosition(date, observerInfo);
            }
            else
            {
                sepObjectPosition = AstroUtil.GetSunPosition(date, observerInfo);
            }

            var sepObjectRaRadians = AstroUtil.ToRadians(AstroUtil.HoursToDegrees(sepObjectPosition.RA));
            var sepObjectDecRadians = AstroUtil.ToRadians(sepObjectPosition.Dec);

            _ = targetCoordinates.Transform(Epoch.JNOW);
            var targetRaRadians = AstroUtil.ToRadians(targetCoordinates.RADegrees);
            var targetDecRadians = AstroUtil.ToRadians(targetCoordinates.Dec);

            var theta = SOFA.Seps(sepObjectRaRadians, sepObjectDecRadians, targetRaRadians, targetDecRadians);

            Separation = AstroUtil.ToDegree(theta);
        }
    }
}