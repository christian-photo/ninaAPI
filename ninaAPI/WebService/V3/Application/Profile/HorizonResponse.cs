#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Reflection;
using NINA.Core.Model;

namespace ninaAPI.WebService.V3.Application.Profile
{
    public class HorizonResponse
    {
        public double[] Altitudes { get; set; }
        public double[] Azimuths { get; set; }

        public HorizonResponse(CustomHorizon horizon)
        {
            if (horizon == null)
            {
                Altitudes = [];
                Azimuths = [];
                return;
            }
            Altitudes = horizon.GetType().GetField("altitudes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(horizon) as double[];
            Azimuths = horizon.GetType().GetField("azimuths", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(horizon) as double[];
        }
    }

}
