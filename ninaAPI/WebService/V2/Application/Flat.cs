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
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FlatDevice;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        private static Task flatTask;
        private static CancellationTokenSource flatCancellationToken;
        private static SequentialContainer container;

        [Route(HttpVerbs.Get, "/flats/skyflat")]
        public void SkyFlats([QueryField] int count,
                            [QueryField] double minExposure,
                            [QueryField] double maxExposure,
                            [QueryField] double histogramMean,
                            [QueryField] double meanTolerance,
                            [QueryField] bool dither,
                            [QueryField] int filterId,
                            [QueryField] string binning,
                            [QueryField] int gain,
                            [QueryField] int offset)
        {
            HttpResponse response = new HttpResponse();

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
                                                AdvancedAPI.Controls.TwilightCalculator);

                    flats.GetIterations().Iterations = count;
                    flats.MaxExposure = HttpContext.IsParameterOmitted(nameof(maxExposure)) ? flats.MaxExposure : maxExposure;
                    flats.MinExposure = HttpContext.IsParameterOmitted(nameof(minExposure)) ? flats.MinExposure : minExposure;
                    flats.HistogramTargetPercentage = HttpContext.IsParameterOmitted(nameof(histogramMean)) ? flats.HistogramTargetPercentage : histogramMean;
                    flats.HistogramTolerancePercentage = HttpContext.IsParameterOmitted(nameof(meanTolerance)) ? flats.HistogramTolerancePercentage : meanTolerance;
                    flats.ShouldDither = HttpContext.IsParameterOmitted(nameof(dither)) ? flats.ShouldDither : dither;
                    flats.GetExposureItem().Gain = HttpContext.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = HttpContext.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!HttpContext.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!HttpContext.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).Any())
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).First();
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !HttpContext.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        HttpContext.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !HttpContext.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        HttpContext.WriteToResponse(response);
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

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/flats/auto-brightness")]
        public void AutoBrightnessFlats([QueryField] int count,
                                        [QueryField] int minBrightness,
                                        [QueryField] int maxBrightness,
                                        [QueryField] double histogramMean,
                                        [QueryField] double meanTolerance,
                                        [QueryField] int filterId,
                                        [QueryField] string binning,
                                        [QueryField] int gain,
                                        [QueryField] int offset,
                                        [QueryField] double exposureTime,
                                        [QueryField] bool keepClosed)
        {
            HttpResponse response = new HttpResponse();

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
                    flats.MaxBrightness = HttpContext.IsParameterOmitted(nameof(maxBrightness)) ? flats.MaxBrightness : maxBrightness;
                    flats.MinBrightness = HttpContext.IsParameterOmitted(nameof(minBrightness)) ? flats.MinBrightness : minBrightness;
                    flats.HistogramTargetPercentage = HttpContext.IsParameterOmitted(nameof(histogramMean)) ? flats.HistogramTargetPercentage : histogramMean;
                    flats.HistogramTolerancePercentage = HttpContext.IsParameterOmitted(nameof(meanTolerance)) ? flats.HistogramTolerancePercentage : meanTolerance;
                    flats.GetExposureItem().Gain = HttpContext.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = HttpContext.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.GetExposureItem().ExposureTime = HttpContext.IsParameterOmitted(nameof(exposureTime)) ? flats.GetExposureItem().ExposureTime : exposureTime;
                    flats.KeepPanelClosed = HttpContext.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!HttpContext.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!HttpContext.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).Any())
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).First();
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !HttpContext.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        HttpContext.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !HttpContext.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        HttpContext.WriteToResponse(response);
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

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/flats/auto-exposure")]
        public void AutoExposureFlats([QueryField] int count,
                                    [QueryField] double minExposure,
                                    [QueryField] double maxExposure,
                                    [QueryField] double histogramMean,
                                    [QueryField] double meanTolerance,
                                    [QueryField] int brightness,
                                    [QueryField] int filterId,
                                    [QueryField] string binning,
                                    [QueryField] int gain,
                                    [QueryField] int offset,
                                    [QueryField] double exposureTime,
                                    [QueryField] bool keepClosed)
        {
            HttpResponse response = new HttpResponse();

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
                    flats.MaxExposure = HttpContext.IsParameterOmitted(nameof(minExposure)) ? flats.MaxExposure : maxExposure;
                    flats.MinExposure = HttpContext.IsParameterOmitted(nameof(minExposure)) ? flats.MinExposure : minExposure;
                    flats.GetSetBrightnessItem().Brightness = HttpContext.IsParameterOmitted(nameof(brightness)) ? flats.GetSetBrightnessItem().Brightness : brightness;
                    flats.HistogramTargetPercentage = HttpContext.IsParameterOmitted(nameof(histogramMean)) ? flats.HistogramTargetPercentage : histogramMean;
                    flats.HistogramTolerancePercentage = HttpContext.IsParameterOmitted(nameof(meanTolerance)) ? flats.HistogramTolerancePercentage : meanTolerance;
                    flats.GetExposureItem().Gain = HttpContext.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = HttpContext.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.GetExposureItem().ExposureTime = HttpContext.IsParameterOmitted(nameof(exposureTime)) ? flats.GetExposureItem().ExposureTime : exposureTime;
                    flats.KeepPanelClosed = HttpContext.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!HttpContext.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!HttpContext.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).Any())
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).First();
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !HttpContext.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        HttpContext.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !HttpContext.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        HttpContext.WriteToResponse(response);
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

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/flats/trained-dark-flat")]
        public void TrainedDarkFlat([QueryField] int count,
                                    [QueryField] int filterId,
                                    [QueryField] string binning,
                                    [QueryField] int gain,
                                    [QueryField] int offset,
                                    [QueryField] bool keepClosed)
        {
            HttpResponse response = new HttpResponse();

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
                    flats.GetExposureItem().Gain = HttpContext.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = HttpContext.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.KeepPanelClosed = HttpContext.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!HttpContext.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!HttpContext.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).Any())
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).First();
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !HttpContext.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        HttpContext.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !HttpContext.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        HttpContext.WriteToResponse(response);
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

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/flats/trained-flat")]
        public void TrainedFlat([QueryField] int count,
                                [QueryField] int filterId,
                                [QueryField] string binning,
                                [QueryField] int gain,
                                [QueryField] int offset,
                                [QueryField] bool keepClosed)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                if (!flatTask?.IsCompleted ?? false)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Process already running", 400));
                    HttpContext.WriteToResponse(response);
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
                    flats.GetExposureItem().Gain = HttpContext.IsParameterOmitted(nameof(gain)) ? flats.GetExposureItem().Gain : gain;
                    flats.GetExposureItem().Offset = HttpContext.IsParameterOmitted(nameof(offset)) ? flats.GetExposureItem().Offset : offset;
                    flats.KeepPanelClosed = HttpContext.IsParameterOmitted(nameof(keepClosed)) ? flats.KeepPanelClosed : keepClosed;

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    if (!HttpContext.IsParameterOmitted(nameof(filterId)))
                    {
                        if (filterId < 0 || filterId >= profile.FilterWheelSettings.FilterWheelFilters.Count)
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Filter not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetSwitchFilterItem().Filter = profile.FilterWheelSettings.FilterWheelFilters[filterId];
                        }
                    }

                    if (!HttpContext.IsParameterOmitted(nameof(binning)))
                    {
                        if (!AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).Any())
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Binning not available", 400));
                            HttpContext.WriteToResponse(response);
                            return;
                        }
                        else
                        {
                            flats.GetExposureItem().Binning = AdvancedAPI.Controls.Camera.GetInfo().BinningModes.Where(b => b.Name == binning).First();
                        }
                    }

                    if ((gain < 0 || gain > AdvancedAPI.Controls.Camera.GetInfo().GainMax) && !HttpContext.IsParameterOmitted(nameof(gain)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid gain", 400));
                        HttpContext.WriteToResponse(response);
                        return;
                    }

                    if ((offset < 0 || offset > AdvancedAPI.Controls.Camera.GetInfo().OffsetMax) && !HttpContext.IsParameterOmitted(nameof(offset)))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid offset", 400));
                        HttpContext.WriteToResponse(response);
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

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/flats/status")]
        public void FlatsStatus()
        {
            HttpResponse response = new HttpResponse();
            try
            {
                response.Response = new FlatStatusResponse(container, flatTask);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/flats/stop")]
        public void FlatsStop()
        {
            HttpResponse response = new HttpResponse();

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

            HttpContext.WriteToResponse(response);
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