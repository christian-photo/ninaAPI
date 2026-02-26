#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Service
{
    public class PlateSolveService
    {
        public PlateSolveService(IImageDataFactory imageDataFactory, IPlateSolverFactory plateSolverFactory, IPlateSolveSettings settings, IApplicationStatusMediator statusMediator)
        {
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.settings = settings;
            this.statusMediator = statusMediator;
        }

        private readonly IImageDataFactory imageDataFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly IPlateSolveSettings settings;
        private readonly IApplicationStatusMediator statusMediator;

        public async Task<PlateSolveResult> PlateSolve(string imagePath, PlatesolveConfig config, double pixelSize, Coordinates coordinates, CancellationToken cts, IProfile profile, int bitDepth = 16, bool isBayered = false)
        {
            IImageData imageData = await Retry.Do(
                async () => await imageDataFactory.CreateFromFile(
                    imagePath,
                    bitDepth,
                    isBayered,
                    config.RawConverter ?? profile.CameraSettings.RawConverter
                ),
                TimeSpan.FromMilliseconds(200), 10
            );

            return await PlateSolve(imageData, config, pixelSize, coordinates, profile, cts);
        }

        public async Task<PlateSolveResult> PlateSolve(IImageData imageData, PlatesolveConfig config, double pixelSize, Coordinates coordinates, IProfile profile, CancellationToken cts)
        {
            CaptureSolverParameter solverParameter = new CaptureSolverParameter()
            {
                Attempts = config.Attempts.Value,
                Binning = config.Binning.Value,
                BlindFailoverEnabled = config.BlindFailoverEnabled.Value,
                Coordinates = coordinates,
                DownSampleFactor = config.DownSampleFactor.Value,
                FocalLength = config.FocalLength.Value,
                MaxObjects = config.MaxObjects.Value,
                Regions = config.Regions.Value,
                SearchRadius = config.SearchRadius.Value,
                PixelSize = pixelSize
            };

            return await PlateSolve(imageData, solverParameter, cts);
        }

        public async Task<PlateSolveResult> PlateSolve(IImageData imageData, CaptureSolverParameter parameter, CancellationToken cts)
        {
            IImageSolver captureSolver = plateSolverFactory.GetImageSolver(
                plateSolverFactory.GetPlateSolver(settings),
                plateSolverFactory.GetBlindSolver(settings)
            );
            var result = await captureSolver.Solve(imageData, parameter, statusMediator.GetStatus(), cts);

            return result;
        }
    }
}
