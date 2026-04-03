#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using NINA.Astrometry.Interfaces;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Flat
{
    public class FlatController : IHttpController
    {
        private readonly ICameraMediator camera;
        private readonly IProfileService profileService;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;
        private readonly IFlatDeviceMediator flatDevice;
        private readonly ITelescopeMediator mount;
        private readonly IImagingMediator imaging;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImageHistoryVM imageHistory;
        private readonly IFilterWheelMediator filterWheel;
        private readonly ITwilightCalculator twilightCalculator;
        private readonly ISymbolBroker symbolBroker;
        private readonly IApplicationStatusMediator applicationStatus;

        public FlatController(
            ICameraMediator camera,
            IProfileService profileService,
            ApiProcessMediator processMediator,
            ISerializerService serializer,
            IFlatDeviceMediator flatDevice,
            ITelescopeMediator mount,
            IImagingMediator imaging,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistory,
            IFilterWheelMediator filterWheel,
            ITwilightCalculator twilightCalculator,
            ISymbolBroker symbolBroker,
            IApplicationStatusMediator applicationStatus
        )
        {
            this.camera = camera;
            this.profileService = profileService;
            this.processMediator = processMediator;
            this.serializer = serializer;
            this.flatDevice = flatDevice;
            this.mount = mount;
            this.imaging = imaging;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistory = imageHistory;
            this.filterWheel = filterWheel;
            this.twilightCalculator = twilightCalculator;
            this.symbolBroker = symbolBroker;
            this.applicationStatus = applicationStatus;
        }

        public object SkyFlats(SkyFlatConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            if (!camera.GetInfo().BinningModes.Any(b => b.Name == config.Binning?.Name))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Binning not available");
            }
            if (!config.Gain?.IsBetween(camera.GetInfo().GainMin, camera.GetInfo().GainMax) ?? false)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(config.Gain), camera.GetInfo().GainMin, camera.GetInfo().GainMax);
            }

            SkyFlat flats = new SkyFlat(
                profileService,
                camera,
                mount,
                imaging,
                imageSaveMediator,
                imageHistory,
                filterWheel,
                twilightCalculator,
                symbolBroker
            );

            flats.GetIterations().Iterations = config.Amount;
            flats.MaxExposure = config.MaxExposure;
            flats.MinExposure = config.MinExposure;
            flats.HistogramTargetPercentage = config.HistogramTargetPercentage;
            flats.HistogramTolerancePercentage = config.MeanTolerance;
            flats.ShouldDither = config.ShouldDither;
            if (config.Gain.HasValue)
            {
                flats.GetExposureItem().Gain = config.Gain.Value;
            }
            if (config.Offset.HasValue)
            {
                flats.GetExposureItem().Offset = config.Offset.Value;
            }
            if (config.FilterId.HasValue && config.FilterId.Value.IsBetween(0, profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1))
            {
                flats.GetSwitchFilterItem().Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[config.FilterId.Value];
            }
            if (config.Binning != null)
            {
                flats.GetExposureItem().Binning = config.Binning;
            }

            if (!flats.Validate())
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Could not start the sky flats instruction because there are issues with the configuration");
            }

            var processId = processMediator.AddProcess(SkyFlatProcess.Create(flats, applicationStatus));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.POST.ToString(), prefix + "/skyfats", (HttpRequest request) => SkyFlats(serializer.Deserialize<SkyFlatConfig>(request.BodyString)));
        }
    }

    public class SkyFlatConfig
    {
        [Range(0, double.MaxValue)]
        [Required]
        public double MinExposure { get; set; }

        [Range(0, double.MaxValue)]
        [Required]
        public double MaxExposure { get; set; }

        [Range(0, 100)]
        [Required]
        public short HistogramTargetPercentage { get; set; }

        [Range(0, 100)]
        [Required]
        public short MeanTolerance { get; set; }

        [Required]
        public bool ShouldDither { get; set; }

        [Range(1, int.MaxValue)]
        [Required]
        public int Amount { get; set; }

        [Range(0, int.MaxValue)]
        public int? FilterId { get; set; }


        public BinningMode Binning { get; set; }

        public int? Gain { get; set; }

        public int? Offset { get; set; }
    }
}