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
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Flat
{
    [Route("/v3/api/flats")]
    public class FlatController : Controller
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

        [Route("POST", "/sky")]
        public object SkyFlats()
        {
            SkyFlatConfig config = serializer.Deserialize<SkyFlatConfig>(Request.BodyString);

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

            if (config.MaxExposure.HasValue) flats.MaxExposure = config.MaxExposure.Value;
            if (config.MinExposure.HasValue) flats.MinExposure = config.MinExposure.Value;
            if (config.HistogramTargetPercentage.HasValue) flats.HistogramTargetPercentage = config.HistogramTargetPercentage.Value;
            if (config.MeanTolerance.HasValue) flats.HistogramTolerancePercentage = config.MeanTolerance.Value;
            if (config.ShouldDither.HasValue) flats.ShouldDither = config.ShouldDither.Value;
            if (config.Gain.HasValue) flats.GetExposureItem().Gain = config.Gain.Value;
            if (config.Offset.HasValue) flats.GetExposureItem().Offset = config.Offset.Value;
            if (config.Binning != null) flats.GetExposureItem().Binning = config.Binning;

            if (config.FilterId.HasValue && config.FilterId.Value.IsBetween(0, profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1))
            {
                flats.GetSwitchFilterItem().Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[config.FilterId.Value];
            }


            if (!flats.Validate())
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Could not start the sky flats instruction because validation failed");
            }

            var processId = processMediator.AddProcess(SkyFlatProcess.Create(flats, applicationStatus));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        [Route("POST", "/auto-brightness")]
        public object AutoBrightnessFlats()
        {
            AutoBrightnessFlatConfig config = serializer.Deserialize<AutoBrightnessFlatConfig>(Request.BodyString);

            Validator.ValidateObject(config, new ValidationContext(config));

            AutoBrightnessFlat flats = new AutoBrightnessFlat(
                profileService,
                camera,
                imaging,
                imageSaveMediator,
                imageHistory,
                filterWheel,
                flatDevice
            );

            flats.GetIterations().Iterations = config.Amount;

            if (config.MaxFlatPanelBrightness?.IsBetween(flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness) ?? false)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(config.MaxFlatPanelBrightness), flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness);
            }
            if (config.MinFlatPanelBrightness?.IsBetween(flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness) ?? false)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(config.MinFlatPanelBrightness), flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness);
            }

            if (config.MaxFlatPanelBrightness.HasValue) flats.MaxBrightness = config.MaxFlatPanelBrightness.Value;
            if (config.MinFlatPanelBrightness.HasValue) flats.MinBrightness = config.MinFlatPanelBrightness.Value;
            if (config.HistogramTargetPercentage.HasValue) flats.HistogramTargetPercentage = config.HistogramTargetPercentage.Value;
            if (config.MeanTolerance.HasValue) flats.HistogramTolerancePercentage = config.MeanTolerance.Value;
            if (config.KeepClosed.HasValue) flats.KeepPanelClosed = config.KeepClosed.Value;
            if (config.Gain.HasValue) flats.GetExposureItem().Gain = config.Gain.Value;
            if (config.Offset.HasValue) flats.GetExposureItem().Offset = config.Offset.Value;
            if (config.Binning != null) flats.GetExposureItem().Binning = config.Binning;

            if (config.FilterId.HasValue && config.FilterId.Value.IsBetween(0, profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1))
            {
                flats.GetSwitchFilterItem().Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[config.FilterId.Value];
            }

            if (!flats.Validate())
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Could not start the auto brightness flats instruction because validation failed");
            }

            var processId = processMediator.AddProcess(AutoBrightnessFlatProcess.Create(flats, applicationStatus));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        [Route("POST", "/auto-exposure")]
        public object AutoExposureFlats()
        {
            AutoExposureFlatConfig config = serializer.Deserialize<AutoExposureFlatConfig>(Request.BodyString);

            Validator.ValidateObject(config, new ValidationContext(config));

            AutoExposureFlat flats = new AutoExposureFlat(
                profileService,
                camera,
                imaging,
                imageSaveMediator,
                imageHistory,
                filterWheel,
                flatDevice
            );

            flats.GetIterations().Iterations = config.Amount;

            if (config.MaxExposure.HasValue) flats.MaxExposure = config.MaxExposure.Value;
            if (config.MinExposure.HasValue) flats.MinExposure = config.MinExposure.Value;
            if (config.HistogramTargetPercentage.HasValue) flats.HistogramTargetPercentage = config.HistogramTargetPercentage.Value;
            if (config.MeanTolerance.HasValue) flats.HistogramTolerancePercentage = config.MeanTolerance.Value;
            if (config.KeepClosed.HasValue) flats.KeepPanelClosed = config.KeepClosed.Value;
            if (config.Gain.HasValue) flats.GetExposureItem().Gain = config.Gain.Value;
            if (config.Offset.HasValue) flats.GetExposureItem().Offset = config.Offset.Value;
            if (config.Binning != null) flats.GetExposureItem().Binning = config.Binning;

            if (config.FilterId.HasValue && config.FilterId.Value.IsBetween(0, profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1))
            {
                flats.GetSwitchFilterItem().Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[config.FilterId.Value];
            }

            if (!flats.Validate())
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Could not start the auto exposure flats instruction because validation failed");
            }

            var processId = processMediator.AddProcess(AutoExposureFlatProcess.Create(flats, applicationStatus));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        [Route("POST", "/trained-dark")]
        public object TrainedDarkFlats()
        {
            TrainedFlatConfig config = serializer.Deserialize<TrainedFlatConfig>(Request.BodyString);

            Validator.ValidateObject(config, new ValidationContext(config));

            TrainedDarkFlatExposure flats = new TrainedDarkFlatExposure(
                profileService,
                camera,
                imaging,
                imageSaveMediator,
                imageHistory,
                filterWheel,
                flatDevice
            );

            flats.GetIterations().Iterations = config.Amount;

            if (config.KeepClosed.HasValue) flats.KeepPanelClosed = config.KeepClosed.Value;
            if (config.Gain.HasValue) flats.GetExposureItem().Gain = config.Gain.Value;
            if (config.Offset.HasValue) flats.GetExposureItem().Offset = config.Offset.Value;
            if (config.Binning != null) flats.GetExposureItem().Binning = config.Binning;

            if (config.FilterId.HasValue && config.FilterId.Value.IsBetween(0, profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1))
            {
                flats.GetSwitchFilterItem().Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[config.FilterId.Value];
            }

            if (!flats.Validate())
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Could not start the trained dark flats exposure flats instruction because validation failed");
            }

            var processId = processMediator.AddProcess(TrainedDarkFlatProcess.Create(flats, applicationStatus));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        [Route("POST", "/trained")]
        public object TrainedFlats()
        {
            TrainedFlatConfig config = serializer.Deserialize<TrainedFlatConfig>(Request.BodyString);

            Validator.ValidateObject(config, new ValidationContext(config));

            TrainedFlatExposure flats = new TrainedFlatExposure(
                profileService,
                camera,
                imaging,
                imageSaveMediator,
                imageHistory,
                filterWheel,
                flatDevice
            );

            flats.GetIterations().Iterations = config.Amount;

            if (config.KeepClosed.HasValue) flats.KeepPanelClosed = config.KeepClosed.Value;
            if (config.Gain.HasValue) flats.GetExposureItem().Gain = config.Gain.Value;
            if (config.Offset.HasValue) flats.GetExposureItem().Offset = config.Offset.Value;
            if (config.Binning != null) flats.GetExposureItem().Binning = config.Binning;

            if (config.FilterId.HasValue && config.FilterId.Value.IsBetween(0, profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1))
            {
                flats.GetSwitchFilterItem().Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[config.FilterId.Value];
            }

            if (!flats.Validate())
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Could not start the trained flats instruction because validation failed");
            }

            var processId = processMediator.AddProcess(TrainedFlatProcess.Create(flats, applicationStatus));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }
    }

    public class TrainedFlatConfig
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Amount { get; set; }

        [Range(0, int.MaxValue)]
        public int? FilterId { get; set; }

        public BinningMode Binning { get; set; }

        public int? Gain { get; set; }

        public int? Offset { get; set; }

        public bool? KeepClosed { get; set; }
    }

    public class AutoExposureFlatConfig
    {
        [Range(0, double.MaxValue)]
        public double? MinExposure { get; set; }

        [Range(0, double.MaxValue)]
        public double? MaxExposure { get; set; }

        [Range(0, 1)]
        public double? HistogramTargetPercentage { get; set; }

        [Range(0, 1)]
        public double? MeanTolerance { get; set; }

        [Range(0, int.MaxValue)]
        public int Brightness { get; set; }

        [Range(0, int.MaxValue)]
        public int? FilterId { get; set; }

        public BinningMode Binning { get; set; }

        public int? Gain { get; set; }

        public int? Offset { get; set; }

        public bool? KeepClosed { get; set; }

        [Range(0, int.MaxValue)]
        [Required]
        public int Amount { get; set; }
    }

    public class AutoBrightnessFlatConfig
    {
        public int? MinFlatPanelBrightness { get; set; }
        public int? MaxFlatPanelBrightness { get; set; }

        [Range(0, 1)]
        public double? HistogramTargetPercentage { get; set; }

        [Range(0, 1)]
        public double? MeanTolerance { get; set; }

        [Range(0, double.MaxValue)]
        public double? ExposureTime { get; set; }

        public bool? KeepClosed { get; set; }

        [Range(0, int.MaxValue)]
        [Required]
        public int Amount { get; set; }

        [Range(0, int.MaxValue)]
        public int? FilterId { get; set; }

        public BinningMode Binning { get; set; }

        public int? Gain { get; set; }

        public int? Offset { get; set; }
    }

    public class SkyFlatConfig
    {
        [Range(0, double.MaxValue)]
        public double? MinExposure { get; set; }

        [Range(0, double.MaxValue)]
        public double? MaxExposure { get; set; }

        [Range(0, 1)]
        public double? HistogramTargetPercentage { get; set; }

        [Range(0, 1)]
        public double? MeanTolerance { get; set; }

        public bool? ShouldDither { get; set; }

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
