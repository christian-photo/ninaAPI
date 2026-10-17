#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FlatDevice;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using SimpleW;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        private static Task flatTask;
        private static CancellationTokenSource flatCancellationToken;
        private static SequentialContainer container;

        [Route("GET", "/flats/skyflat")]
        public void SkyFlats(int count = 10,
                            double minExposure = -1,
                            double maxExposure = -1,
                            double histogramMean = -1,
                            double meanTolerance = -1,
                            bool dither = false,
                            int filterId = -1,
                            string binning = "1x1",
                            int gain = -1,
                            int offset = -1)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!flatTask?.IsCompleted ?? false)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Process already running", 400));
                }
                else
                {
                    SkyFlat flats = new SkyFlat(AdvancedAPI.Controls.Profile,
                                                AdvancedAPI.Controls.Camera,
                                                AdvancedAPI.Controls.Mount,
                                                AdvancedAPI.Controls.Imaging,
                                                AdvancedAPI.Controls.ImageSaveMediator,
                                                AdvancedAPI.Controls.ImageHistory,
                                                AdvancedAPI.Controls.FilterWheel,
                                                AdvancedAPI.Controls.TwilightCalculator,
                                                AdvancedAPI.Controls.SymbolBroker);

                    flats.GetIterations().Iterations = count;
                    flats.MaxExposure = Request.IsParameterOmitted(nameof(maxExposure)) ? flats.MaxExposure : maxExposure;
                    flats.MinExposure = Request.IsParameterOmitted(nameof(minExposure)) ? flats.MinExposure : minExposure;
                    flats.HistogramTargetPercentage = Request.IsParameterOmitted(nameof(histogramMean)) ? flats.HistogramTargetPercentage : histogramMean;
                    flats.HistogramTolerancePercentage = Request.IsParameterOmitted(nameof(meanTolerance)) ? flats.HistogramTolerancePercentage : meanTolerance;
                    flats.ShouldDither = Request.IsParameterOmitted(nameof(dither)) ? flats.ShouldDither : dither;
                    flats.GetExposureItem().Gain = Request.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = Request.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!Request.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!Request.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Any(b => b.Name == binning))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.First(b => b.Name == binning);
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !Request.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !Request.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if (flats.Validate())
                    {
                        container = flats;
                        flatCancellationToken = new CancellationTokenSource();
                        flatTask = flats.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), flatCancellationToken.Token);
                        response.Response = "Process started";
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Issues found", 400));
                        response.Response = flats.Issues;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/flats/auto-brightness")]
        public void AutoBrightnessFlats(int count = 10,
                                        int minBrightness = -1,
                                        int maxBrightness = -1,
                                        double histogramMean = -1,
                                        double meanTolerance = -1,
                                        int filterId = -1,
                                        string binning = "1x1",
                                        int gain = -1,
                                        int offset = -1,
                                        double exposureTime = -1,
                                        bool keepClosed = false)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!flatTask?.IsCompleted ?? false)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Process already running", 400));
                }
                else
                {
                    AutoBrightnessFlat flats = new AutoBrightnessFlat(AdvancedAPI.Controls.Profile,
                                                        AdvancedAPI.Controls.Camera,
                                                        AdvancedAPI.Controls.Imaging,
                                                        AdvancedAPI.Controls.ImageSaveMediator,
                                                        AdvancedAPI.Controls.ImageHistory,
                                                        AdvancedAPI.Controls.FilterWheel,
                                                        AdvancedAPI.Controls.FlatDevice);

                    flats.GetIterations().Iterations = count;
                    flats.MaxBrightness = Request.IsParameterOmitted(nameof(maxBrightness)) ? flats.MaxBrightness : maxBrightness;
                    flats.MinBrightness = Request.IsParameterOmitted(nameof(minBrightness)) ? flats.MinBrightness : minBrightness;
                    flats.HistogramTargetPercentage = Request.IsParameterOmitted(nameof(histogramMean)) ? flats.HistogramTargetPercentage : histogramMean;
                    flats.HistogramTolerancePercentage = Request.IsParameterOmitted(nameof(meanTolerance)) ? flats.HistogramTolerancePercentage : meanTolerance;
                    flats.GetExposureItem().Gain = Request.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = Request.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.GetExposureItem().ExposureTime = Request.IsParameterOmitted(nameof(exposureTime)) ? flats.GetExposureItem().ExposureTime : exposureTime;
                    flats.KeepPanelClosed = Request.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!Request.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!Request.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Any(b => b.Name == binning))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.First(b => b.Name == binning);
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !Request.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !Request.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if (flats.Validate())
                    {
                        container = flats;
                        flatCancellationToken = new CancellationTokenSource();
                        flatTask = flats.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), flatCancellationToken.Token);
                        response.Response = "Process started";
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Issues found", 400));
                        response.Response = flats.Issues;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/flats/auto-exposure")]
        public void AutoExposureFlats(int count = 10,
                                    double minExposure = -1,
                                    double maxExposure = -1,
                                    double histogramMean = -1,
                                    double meanTolerance = -1,
                                    int brightness = -1,
                                    int filterId = -1,
                                    string binning = "1x1",
                                    int gain = -1,
                                    int offset = -1,
                                    double exposureTime = -1,
                                    bool keepClosed = false)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!flatTask?.IsCompleted ?? false)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Process already running", 400));
                }
                else
                {
                    AutoExposureFlat flats = new AutoExposureFlat(AdvancedAPI.Controls.Profile,
                                                                AdvancedAPI.Controls.Camera,
                                                                AdvancedAPI.Controls.Imaging,
                                                                AdvancedAPI.Controls.ImageSaveMediator,
                                                                AdvancedAPI.Controls.ImageHistory,
                                                                AdvancedAPI.Controls.FilterWheel,
                                                                AdvancedAPI.Controls.FlatDevice);

                    flats.GetIterations().Iterations = count;
                    flats.MaxExposure = Request.IsParameterOmitted(nameof(minExposure)) ? flats.MaxExposure : maxExposure;
                    flats.MinExposure = Request.IsParameterOmitted(nameof(minExposure)) ? flats.MinExposure : minExposure;
                    flats.GetSetBrightnessItem().Brightness = Request.IsParameterOmitted(nameof(brightness)) ? flats.GetSetBrightnessItem().Brightness : brightness;
                    flats.HistogramTargetPercentage = Request.IsParameterOmitted(nameof(histogramMean)) ? flats.HistogramTargetPercentage : histogramMean;
                    flats.HistogramTolerancePercentage = Request.IsParameterOmitted(nameof(meanTolerance)) ? flats.HistogramTolerancePercentage : meanTolerance;
                    flats.GetExposureItem().Gain = Request.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = Request.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.GetExposureItem().ExposureTime = Request.IsParameterOmitted(nameof(exposureTime)) ? flats.GetExposureItem().ExposureTime : exposureTime;
                    flats.KeepPanelClosed = Request.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!Request.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!Request.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Any(b => b.Name == binning))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.First(b => b.Name == binning);
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !Request.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !Request.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if (flats.Validate())
                    {
                        container = flats;
                        flatCancellationToken = new CancellationTokenSource();
                        flatTask = flats.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), flatCancellationToken.Token);
                        response.Response = "Process started";
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Issues found", 400));
                        response.Response = flats.Issues;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/flats/trained-dark-flat")]
        public void TrainedDarkFlat(int count = 10,
                                    int filterId = -1,
                                    string binning = "1x1",
                                    int gain = -1,
                                    int offset = -1,
                                    bool keepClosed = false)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!flatTask?.IsCompleted ?? false)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Process already running", 400));
                }
                else
                {
                    TrainedDarkFlatExposure flats = new TrainedDarkFlatExposure(AdvancedAPI.Controls.Profile,
                                                                AdvancedAPI.Controls.Camera,
                                                                AdvancedAPI.Controls.Imaging,
                                                                AdvancedAPI.Controls.ImageSaveMediator,
                                                                AdvancedAPI.Controls.ImageHistory,
                                                                AdvancedAPI.Controls.FilterWheel,
                                                                AdvancedAPI.Controls.FlatDevice);

                    flats.GetIterations().Iterations = count;
                    flats.GetExposureItem().Gain = Request.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = Request.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.KeepPanelClosed = Request.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!Request.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!Request.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Any(b => b.Name == binning))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.First(b => b.Name == binning);
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !Request.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !Request.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if (flats.Validate())
                    {
                        container = flats;
                        flatCancellationToken = new CancellationTokenSource();
                        flatTask = flats.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), flatCancellationToken.Token);
                        response.Response = "Process started";
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Issues found", 400));
                        response.Response = flats.Issues;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/flats/trained-flat")]
        public void TrainedFlat(int count = 10,
                                int filterId = -1,
                                string binning = "1x1",
                                int gain = -1,
                                int offset = -1,
                                bool keepClosed = false)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!flatTask?.IsCompleted ?? false)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Process already running", 400));
                    Response.WriteToResponse(response);
                    return;
                }
                if (!AdvancedAPI.Controls.Camera.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Camera not connected", 400));
                }
                else
                {
                    TrainedFlatExposure flats = new TrainedFlatExposure(AdvancedAPI.Controls.Profile,
                                                                        AdvancedAPI.Controls.Camera,
                                                                        AdvancedAPI.Controls.Imaging,
                                                                        AdvancedAPI.Controls.ImageSaveMediator,
                                                                        AdvancedAPI.Controls.ImageHistory,
                                                                        AdvancedAPI.Controls.FilterWheel,
                                                                        AdvancedAPI.Controls.FlatDevice);

                    flats.GetIterations().Iterations = count;
                    flats.GetExposureItem().Gain = Request.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = Request.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.KeepPanelClosed = Request.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!Request.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!Request.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Any(b => b.Name == binning))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            Response.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.First(b => b.Name == binning);
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !Request.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !Request.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        Response.WriteToResponse(response);
                        return;
                    }

                    if (flats.Validate())
                    {
                        container = flats;
                        flatCancellationToken = new CancellationTokenSource();
                        flatTask = flats.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), flatCancellationToken.Token);
                        response.Response = "Process started";
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Issues found", 400));
                        response.Response = flats.Issues;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/flats/status")]
        public void FlatsStatus()
        {
            CustomResponse response = new CustomResponse();
            try
            {
                response.Response = new FlatStatusResponse(container, flatTask);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/flats/stop")]
        public void FlatsStop()
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (flatTask is not null && flatCancellationToken is not null)
                {
                    flatCancellationToken.Cancel();
                    response.Response = "Process stopped";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("No process running", 400));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }
    }

    public struct FlatStatusResponse
    {
        public string State { get; }
        public int TotalIterations { get; }
        public int CompletedIterations { get; }

        public FlatStatusResponse(SequentialContainer container, Task task)
        {
            if (task is not null)
            {
                State = task.IsCompleted ? "Finished" : "Running";
            }
            else
            {
                State = "Finished";
            }

            if (State.Equals("Running"))
            {
                LoopCondition loop = (LoopCondition)container.GetType().GetMethod("GetIterations").Invoke(container, null);
                TotalIterations = loop.Iterations;
                CompletedIterations = loop.CompletedIterations;
            }
            else
            {
                TotalIterations = -1;
                CompletedIterations = -1;
            }
        }
    }
}
