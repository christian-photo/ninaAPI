#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Concurrent;
using Accord;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CaptureMediator
    {
        public CaptureMediator(ICameraMediator camera, IFilterWheelMediator filterWheel, IProfileService profile, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IApplicationStatusMediator statusMediator, ApiProcessMediator processMediator)
        {
            this.camera = camera;
            this.filterWheel = filterWheel;
            this.profile = profile;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.statusMediator = statusMediator;
            this.processMediator = processMediator;

            this.captures = new();
        }
        private readonly ICameraMediator camera;
        private readonly IFilterWheelMediator filterWheel;
        private readonly IProfileService profile;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ApiProcessMediator processMediator;

        private readonly ConcurrentDictionary<Guid, Capture> captures;

        public Capture AddCapture()
        {
            Capture capture = new Capture(camera, filterWheel, profile, imagingMediator, imageSaveMediator, statusMediator, processMediator);
            captures.AddOrUpdate(
                capture.CaptureId,
                capture,
                (_, existing) => existing
            );

            return capture;
        }

        public Capture GetCapture(Guid id)
        {
            return captures.TryGetValue(id, out var capture) ? capture : null;
        }

        public void RemoveCapture(Guid id)
        {
            captures.TryRemove(id, out var capture);
            capture?.Cleanup();
            processMediator.RemoveProcess(id);
        }
    }
}
