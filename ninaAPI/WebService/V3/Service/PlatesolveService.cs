#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Service
{
    public class PlateSolveService
    {
        public PlateSolveService(IImageDataFactory imageDataFactory, IPlateSolverFactory plateSolverFactory, IPlateSolveSettings settings)
        {
            this.imageDataFactory = imageDataFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.settings = settings;
        }

        private readonly IImageDataFactory imageDataFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly IPlateSolveSettings settings;

        public async Task<PlateSolveResult> PlateSolve(string imagePath, PlatesolveConfig config, double pixelSize, Coordinates coordinates, int bitDepth = 16, bool isBayered = false)
        {
            IImageData imageData = await Retry.Do(
                async () => await imageDataFactory.CreateFromFile(
                    imagePath,
                    bitDepth,
                    isBayered,
                    config.RawConverter ?? NINA.Core.Enum.RawConverterEnum.FREEIMAGE
                ),
                TimeSpan.FromMilliseconds(200), 10
            );
            return PlateSolve(imageData, config, pixelSize, coordinates);
        }

        public PlateSolveResult PlateSolve(IImageData imageData, PlatesolveConfig config, double pixelSize, Coordinates coordinates)
        {
            CaptureSolverParameter solverParameter = new CaptureSolverParameter()
            {
                Attempts = config.Attempts ?? 1,
                Binning = config.Binning ?? 1,
                BlindFailoverEnabled = config.BlindFailoverEnabled ?? false,
                Coordinates = coordinates,
                DownSampleFactor = config.DownSampleFactor ?? 1,
                FocalLength = 0,
                MaxObjects = config.MaxObjects ?? 1,
                Regions = config.Regions ?? 0,
                SearchRadius = config.SearchRadius ?? 0,
                PixelSize = pixelSize
            };

            return PlateSolve(imageData, solverParameter);
        }

        public async Task<PlateSolveResult> PlateSolve(IImageData imageData, CaptureSolverParameter parameter)
        {
            IImageSolver captureSolver = plateSolverFactory.GetImageSolver(
                plateSolverFactory.GetPlateSolver(settings),
                plateSolverFactory.GetBlindSolver(settings)
            );
            var result = await captureSolver.Solve(imageData, parameter, statusMediator.GetStatus(), HttpContext.CancellationToken);
        }
    }
}