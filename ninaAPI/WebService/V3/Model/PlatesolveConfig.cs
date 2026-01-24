#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using Swan.Validators;

namespace ninaAPI.WebService.V3.Model
{
    public class HttpCoordinates
    {
        [Range(-180, 180)]
        public double? RA { get; set; }

        [Range(-90, 90)]
        public double? Dec { get; set; }

        public Epoch? Epoch { get; set; }

        public Coordinates ToCoordinates()
        {
            return new Coordinates(Angle.ByDegree((double)RA), Angle.ByDegree((double)Dec), Epoch ?? NINA.Astrometry.Epoch.J2000);
        }
    }
    public class PlatesolveConfig
    {
        [Range(1, int.MaxValue)]
        public int? Attempts { get; set; }

        [Range(1, int.MaxValue)]
        public int? DownSampleFactor { get; set; }

        public bool? BlindFailoverEnabled { get; set; }

        [Range(0, double.MaxValue)]
        public double? SearchRadius { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxObjects { get; set; }

        [Range(1, short.MaxValue)]
        public short? Binning { get; set; }

        [Range(0, int.MaxValue)]
        public int? Regions { get; set; }

        [Range(10, double.MaxValue)]
        public double? FocalLength { get; set; }

        public RawConverterEnum? RawConverter { get; set; }

        public HttpCoordinates? Coordinates { get; set; }

        [Range(0, double.MaxValue)]
        public double? PixelSize { get; set; }

        public void UpdateDefaults(IProfile profile, ITelescopeMediator mount, ICameraMediator camera)
        {
            Attempts ??= profile.PlateSolveSettings.NumberOfAttempts;
            DownSampleFactor ??= profile.PlateSolveSettings.DownSampleFactor;
            BlindFailoverEnabled ??= profile.PlateSolveSettings.BlindFailoverEnabled;
            SearchRadius ??= profile.PlateSolveSettings.SearchRadius;
            MaxObjects ??= profile.PlateSolveSettings.MaxObjects;
            Binning ??= profile.PlateSolveSettings.Binning;
            Regions ??= profile.PlateSolveSettings.Regions;
            FocalLength ??= profile.TelescopeSettings.FocalLength;
            RawConverter ??= profile.CameraSettings.RawConverter;
            Coordinates ??= new HttpCoordinates();
            Coordinates.RA ??= mount.GetCurrentPosition().RA;
            Coordinates.Dec ??= mount.GetCurrentPosition().Dec;
            Coordinates.Epoch = Epoch.J2000;
            PixelSize ??= camera.GetInfo().PixelSize;
        }
    }
}
