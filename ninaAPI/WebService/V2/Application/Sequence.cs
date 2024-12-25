#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using ninaAPI.Utility;
using NINA.Core.Utility;
using NINA.Sequencer.Interfaces.Mediator;
using System.Collections.Generic;
using NINA.Sequencer.Container;
using System.Collections;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Sequencer.Trigger.Platesolving;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Sequencer.SequenceItem.Telescope;
using System.IO;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/sequence/json")]
        public void SequenceJson()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator Sequence = AdvancedAPI.Controls.Sequence;

                if (!Sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else
                {
                    IList<IDeepSkyObjectContainer> targets = Sequence.GetAllTargets();
                    if (targets.Count == 0)
                    {
                        response = CoreUtility.CreateErrorTable(new Error("No DSO Container found", 409));
                    }
                    else
                    {
                        response.Response = getSequenceRecursivley(targets[0].Parent.Parent);
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

        private static List<Hashtable> getSequenceRecursivley(ISequenceContainer sequence)
        {
            List<Hashtable> result = new List<Hashtable>();
            foreach (var item in sequence.Items)
            {
                Hashtable it = new Hashtable
                {
                    { "Name", item.Name },
                    { "Status", item.Status.ToString() },
                    // { "Description", item.Description }
                };

                if (item is ISequenceTrigger t)
                {
                    it["Name"] = item.Name + "_Trigger";
                }
                else if (item is ISequenceCondition c)
                {
                    it["Name"] = item.Name + "_Condition";
                }
                else if (item is ISequenceContainer container && item is not SmartExposure && item is not TakeManyExposures)
                {
                    it["Name"] = item.Name + "_Container";
                    it.Add("Items", getSequenceRecursivley(container));
                }

                if (item is AutofocusAfterExposures trigger)
                {
                    it.Add("Exposures", trigger.ProgressExposures);
                    it.Add("DeltaExposures", trigger.AfterExposures);
                }
                else if (item is AutofocusAfterHFRIncreaseTrigger t2)
                {
                    it.Add("HFRTrendPercentage", t2.HFRTrendPercentage);
                    it.Add("OriginalHFR", t2.OriginalHFR);
                    it.Add("SampleSize", t2.SampleSize);
                    it.Add("DeltaHFR", t2.Amount);
                }
                else if (item is AutofocusAfterTemperatureChangeTrigger t3)
                {
                    it.Add("DeltaTemperature", t3.DeltaT);
                    it.Add("TargetTemperature", t3.Amount);
                }
                else if (item is AutofocusAfterTimeTrigger t4)
                {
                    it.Add("DeltaTime", t4.Amount);
                    it.Add("ElapsedTime", t4.Elapsed);
                }
                else if (item is DitherAfterExposures t5)
                {
                    it.Add("Exposures", t5.ProgressExposures);
                    it.Add("TargetExposures", t5.AfterExposures);
                }
                else if (item is MeridianFlipTrigger t6)
                {
                    it.Add("TimeToFlip", t6.TimeToMeridianFlip);
                }
                else if (item is CenterAfterDriftTrigger t7)
                {
                    it.Add("Coordinates", t7.Coordinates);
                    it.Add("Drift", t7.LastDistanceArcMinutes);
                    it.Add("TargetDrift", t7.DistanceArcMinutes);
                }
                else if (item is TimeCondition c1)
                {
                    it.Add("RemainingTime", c1.RemainingTime);
                    it.Add("DateTime", c1.DateTime);
                }
                else if (item is LoopForAltitudeBase c2)
                {
                    it.Add("Altitude", c2.Data.Offset);
                    it.Add("CurrentAltitude", c2.Data.CurrentAltitude);
                    it.Add("ExpectedTime", c2.Data.ExpectedTime);
                }
                else if (item is LoopCondition c3)
                {
                    it.Add("Iterations", c3.Iterations);
                    it.Add("CompletedIterations", c3.CompletedIterations);
                }
                else if (item is MoonIlluminationCondition c5)
                {
                    it.Add("TargetIllumination", c5.UserMoonIllumination);
                    it.Add("CurrentIllumination", c5.CurrentMoonIllumination);
                }
                else if (item is TimeSpanCondition c6)
                {
                    it.Add("RemainingTime", c6.RemainingTime);
                    it.Add("DateTime", c6.DateTime);
                }
                else if (item is CoolCamera i1)
                {
                    it.Add("Temperature", i1.Temperature);
                    it.Add("MinCoolingTime", i1.Duration);
                }
                else if (item is WarmCamera i2)
                {
                    it.Add("MinWarmingTime", i2.Duration);
                }
                else if (item is DewHeater i3)
                {
                    it.Add("DewHeaterOn", i3.OnOff);
                }
                else if (item is SmartExposure i4)
                {
                    it.Add("ExposureTime", i4.GetTakeExposure().ExposureTime);
                    it.Add("ExposureCount", i4.GetTakeExposure().ExposureCount);
                    it.Add("Binning", i4.GetTakeExposure().Binning);
                    it.Add("Gain", i4.GetTakeExposure().Gain);
                    it.Add("Offset", i4.GetTakeExposure().Offset);
                    it.Add("Type", i4.GetTakeExposure().ImageType);
                    it.Add("DitherProgressExposures", i4.GetDitherAfterExposures().ProgressExposures);
                    it.Add("DitherTargetExposures", i4.GetDitherAfterExposures().AfterExposures);
                    it.Add("Iterations", i4.GetLoopCondition().Iterations);
                    it.Add("Filter", i4.GetSwitchFilter().Filter?.Name ?? "Current");
                }
                else if (item is TakeExposure i5)
                {
                    it.Add("ExposureTime", i5.ExposureTime);
                    it.Add("ExposureCount", i5.ExposureCount);
                    it.Add("Binning", i5.Binning);
                    it.Add("Gain", i5.Gain);
                    it.Add("Offset", i5.Offset);
                    it.Add("Type", i5.ImageType);
                }
                else if (item is TakeManyExposures i6)
                {
                    it.Add("ExposureTime", i6.GetTakeExposure().ExposureTime);
                    it.Add("ExposureCount", i6.GetTakeExposure().ExposureCount);
                    it.Add("Binning", i6.GetTakeExposure().Binning);
                    it.Add("Gain", i6.GetTakeExposure().Gain);
                    it.Add("Offset", i6.GetTakeExposure().Offset);
                    it.Add("Type", i6.GetTakeExposure().ImageType);
                    it.Add("Iterations", i6.GetLoopCondition().Iterations);
                }
                else if (item is TakeSubframeExposure i7)
                {
                    it.Add("ExposureTime", i7.ExposureTime);
                    it.Add("ExposureCount", i7.ExposureCount);
                    it.Add("Binning", i7.Binning);
                    it.Add("Gain", i7.Gain);
                    it.Add("Offset", i7.Offset);
                    it.Add("Type", i7.ImageType);
                    it.Add("ROI", i7.ROI);
                }
                else if (item is SlewDomeAzimuth i8)
                {
                    it.Add("Azimuth", i8.AzimuthDegrees);
                }
                else if (item is SwitchFilter i9)
                {
                    it.Add("Filter", i9.Filter?.Name ?? "Current");
                }
                else if (item is MoveFocuserAbsolute i10)
                {
                    it.Add("Position", i10.Position);
                }
                else if (item is MoveFocuserRelative i11)
                {
                    it.Add("RelativePosition", i11.RelativePosition);
                }
                else if (item is MoveFocuserByTemperature i12)
                {
                    it.Add("Slope", i12.Slope);
                    it.Add("Absolute", i12.Absolute);
                    it.Add("Intercept", i12.Intercept);
                }
                else if (item is StartGuiding i13)
                {
                    it.Add("ForceCalibration", i13.ForceCalibration);
                }
                else if (item is SolveAndRotate i14)
                {
                    it.Add("Rotation", i14.PositionAngle);
                }
                else if (item is SetSwitchValue i15)
                {
                    it.Add("Value", i15.Value);
                    it.Add("Index", i15.SelectedSwitch.Id);
                }
                else if (item is SetTracking i16)
                {
                    it.Add("TrackingMode", i16.TrackingMode);
                }
                else if (item is Center i17)
                {
                    it.Add("Coordinates", i17.Coordinates.Coordinates);
                }
                else if (item is SlewScopeToAltAz i18)
                {
                    it.Add("Coordinates", i18.Coordinates.Coordinates);
                }
                else if (item is SlewScopeToRaDec i19)
                {
                    it.Add("Coordinates", i19.Coordinates.Coordinates);
                }
                else if (item is CenterAndRotate i20)
                {
                    it.Add("Coordinates", i20.Coordinates.Coordinates);
                    it.Add("Rotation", i20.PositionAngle);
                }
                else if (item is Annotation i21)
                {
                    it.Add("Text", i21.Text);
                }
                else if (item is MessageBox i22)
                {
                    it.Add("Text", i22.Text);
                }
                else if (item is ExternalScript i23)
                {
                    it.Add("Script", i23.Script);
                }
                else if (item is SaveSequence i24)
                {
                    it.Add("FilePath", i24.FilePath);
                }
                else if (item is WaitForAltitudeBase i25)
                {
                    it.Add("Altitude", i25.Data.Offset);
                    it.Add("CurrentAltitude", i25.Data.CurrentAltitude);
                    it.Add("ExpectedTime", i25.Data.ExpectedTime);
                }
                else if (item is WaitForTime i26)
                {
                    it.Add("Time", i26.DateTime);
                }
                else if (item is WaitForTimeSpan i27)
                {
                    it.Add("Delay", i27.Time);
                }

                result.Add(it);
            }

            return result;
        }

        [Route(HttpVerbs.Get, "/sequence/start")]
        public void SequenceStart([QueryField] bool skipValidation)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else
                {
                    sequence.StartAdvancedSequence(skipValidation);
                    response.Response = "Sequence started";
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/stop")]
        public void SequenceStop()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else
                {
                    sequence.CancelAdvancedSequence();
                    response.Response = "Sequence stopped";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/load")]
        public void SequenceLoad([QueryField] string sequencename)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                // Use either a name of a sequence or let the user upload a json file

                // ISequenceRootContainer container = JsonConvert.DeserializeObject<SequenceRootContainer>(sequenceJson); // Not working
                // sequence.SetAdvancedSequence(container);

                // response.Response = "Sequence updated";
                response.Response = "Not yet implemented";
                response.Success = false;
                response.StatusCode = 501;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/list-available")]
        public void SequenceGetAvailable()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;

                List<string> f = new List<string>();

                string[] files = Directory.GetFiles(sequenceFolder);
                foreach (string file in files)
                {
                    if (file.EndsWith(".json"))
                    {
                        f.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }

                response.Response = f;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
