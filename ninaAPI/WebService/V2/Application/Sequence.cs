#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Sequencer.Trigger.Platesolving;
using ninaAPI.Utility;

namespace ninaAPI.WebService.V2
{
    public class SequenceWatcher : INinaWatcher
    {
        public void StartWatchers()
        {
            Task.Run(async () =>
            {
                while (!AdvancedAPI.Controls.Sequence?.Initialized ?? true)
                {
                    await Task.Delay(50);
                }
                Logger.Debug("Finished initializing sequence, subscribing to events");
                AdvancedAPI.Controls.Sequence.SequenceStarting += SequenceStarting;
                AdvancedAPI.Controls.Sequence.SequenceFinished += SequenceFinished;
            });
        }

        private async Task SequenceFinished(object arg1, EventArgs args)
        {
            await WebSocketV2.SendAndAddEvent("SEQUENCE-FINISHED");
        }

        private async Task SequenceStarting(object arg1, EventArgs args)
        {
            await WebSocketV2.SendAndAddEvent("SEQUENCE-STARTING");
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Sequence.SequenceStarting -= SequenceStarting;
            AdvancedAPI.Controls.Sequence.SequenceFinished -= SequenceFinished;
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/sequence/json")]
        public void SequenceJson()
        {
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
                    ISequenceRootContainer root = Sequence.GetSequenceRoot();
                    List<Hashtable> json =
                    [
                        new Hashtable() { { "GlobalTriggers", getTriggers((SequenceContainer)root) } },
                        .. getSequenceRecursivley(root),
                    ]; // Global triggers
                    response.Response = json;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/state")]
        public void SequenceState()
        {
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
                    ISequenceRootContainer root = Sequence.GetSequenceRoot();
                    List<Hashtable> json =
                    [
                        new Hashtable() { { "GlobalTriggers", getTriggersNew((SequenceContainer)root) } },
                        .. getSequenceRecursivleyNew(root),
                    ]; // Global triggers
                    response.Response = json;
                }
                HttpContext.WriteSequenceResponse(response);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }


        }

        [Route(HttpVerbs.Get, "/sequence/edit")]
        public void SequenceEdit([QueryField] string path, [QueryField] string value)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 400));
                }
                else if (string.IsNullOrEmpty(path))
                {
                    response = CoreUtility.CreateErrorTable(new Error("Invalid path", 400));
                }
                else if (string.IsNullOrEmpty(value))
                {
                    response = CoreUtility.CreateErrorTable(new Error("New value can't be null", 400));
                }
                else
                {
                    ISequenceRootContainer root = sequence.GetSequenceRoot();
                    string[] pathSplit = path.Split('-'); // e.g. 'CameraSettings-PixelSize' -> CameraSettings, PixelSize
                    object position = AdvancedAPI.Controls.Profile.ActiveProfile;
                    switch (pathSplit[0])
                    {
                        case "GlobalTriggers":
                            position = root.Triggers;
                            break;
                        case "Start":
                            position = root.Items[0];
                            break;
                        case "Imaging":
                            position = root.Items[1];
                            break;
                        case "End":
                            position = root.Items[2];
                            break;
                    }


                    for (int i = 1; i < pathSplit.Length - 1; i++)
                    {
                        if (int.TryParse(pathSplit[i], out int x))
                        {
                            var enumerable = position as IList;
                            position = enumerable[x];
                        }
                        else
                        {
                            position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
                        }
                    }
                    PropertyInfo prop = position.GetType().GetProperty(pathSplit[^1]);
                    prop.SetValue(position, value.CastString(prop.PropertyType));
                    position.GetType().GetMethod("RaisePropertyChanged", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(position, new object[] { pathSplit[^1] });

                    response.Response = "Updated setting";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(new Error(ex.Message, 500));
            }

            HttpContext.WriteToResponse(response);
        }

        private static List<Hashtable> getConditionsNew(SequenceContainer sequence)
        {
            List<Hashtable> conditions = new List<Hashtable>();
            foreach (var condition in sequence.Conditions)
            {
                try
                {
                    Hashtable ctable = new Hashtable
                    {
                        { "Name", condition.Name + "_Condition" },
                        { "Status", condition.Status.ToString() }
                    };
                    var proper = condition.GetType().GetProperties().Where(p => p.MemberType == MemberTypes.Property && !ignoredProperties.Contains(p.Name) && !typeof(SequenceCondition).GetProperties().Any(x => x.Name == p.Name));
                    foreach (var prop in proper)
                    {
                        if (prop.CanWrite && (prop.GetSetMethod(true)?.IsPublic ?? false) && prop.CanRead && (prop.GetGetMethod(true)?.IsPublic ?? false))
                        {
                            ctable.Add(prop.Name, prop.GetValue(condition));
                        }
                    }
                    conditions.Add(ctable);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

            }
            return conditions;
        }

        private static List<Hashtable> getTriggersNew(SequenceContainer sequence)
        {
            List<Hashtable> triggers = new List<Hashtable>();
            foreach (var trigger in sequence.Triggers)
            {
                try
                {
                    Hashtable triggertable = new Hashtable
                {
                    { "Name", trigger.Name + "_Trigger" },
                    { "Status", trigger.Status.ToString() }
                };
                    var proper = trigger.GetType().GetProperties().Where(p => p.MemberType == MemberTypes.Property && !ignoredProperties.Contains(p.Name) && !typeof(SequenceTrigger).GetProperties().Any(x => x.Name == p.Name));
                    foreach (var prop in proper)
                    {
                        if (prop.CanWrite && (prop.GetSetMethod(true)?.IsPublic ?? false) && prop.CanRead && (prop.GetGetMethod(true)?.IsPublic ?? false))
                        {
                            triggertable.Add(prop.Name, prop.GetValue(trigger));
                        }
                    }
                    triggers.Add(triggertable);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

            }
            return triggers;
        }

        private static readonly string[] ignoredProperties = {
            "Name", "Status", "IsExpanded", "ErrorBehavior", "Attempts", "CoordsFromPlanetariumCommand", "ExposureInfoListExpanded", "CoordsToFramingCommand",
            "DeleteExposureInfoCommand", "ExposureInfoList", "DateTimeProviders", "ImageTypes", "DropTargetCommand", "DateTime", "ProfileService", "Parent", "InfoButtonColor", "Icon" };

        private static List<Hashtable> getSequenceRecursivleyNew(ISequenceContainer sequence)
        {
            List<Hashtable> result = new List<Hashtable>();

            foreach (var item in sequence.Items)
            {
                try
                {
                    Hashtable it = new Hashtable
                {
                    { "Name", item.Name },
                    { "Status", item.Status.ToString() },
                };

                    if (item is ISequenceContainer container)
                    {
                        it["Name"] = item.Name + "_Container";
                        it.Add("Items", getSequenceRecursivleyNew(container));
                        if (container is SequenceContainer sc)
                        {
                            it.Add("Conditions", getConditionsNew(sc));
                            it.Add("Triggers", getTriggersNew(sc));
                        }
                    }


                    var proper = item.GetType().GetProperties().Where(p => p.MemberType == MemberTypes.Property && !ignoredProperties.Contains(p.Name) && !typeof(SequenceItem).GetProperties().Any(x => x.Name == p.Name));
                    foreach (var prop in proper)
                    {
                        if ((prop.GetSetMethod(true)?.IsPublic ?? false) && prop.CanRead && (prop.GetGetMethod(true)?.IsPublic ?? false))
                        {
                            it.Add(prop.Name, prop.GetValue(item));
                        }
                    }

                    result.Add(it);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return result;
        }

        private static List<Hashtable> getTriggers(SequenceContainer sequence)
        {
            List<Hashtable> triggers = new List<Hashtable>();
            foreach (var trigger in sequence.Triggers)
            {
                try
                {
                    Hashtable triggertable = new Hashtable
                    {
                        { "Name", trigger.Name + "_Trigger" },
                        { "Status", trigger.Status.ToString() }
                    };
                    if (trigger is AutofocusAfterExposures t1)
                    {
                        triggertable.Add("Exposures", t1.ProgressExposures);
                        triggertable.Add("DeltaExposures", t1.AfterExposures);
                    }
                    else if (trigger is AutofocusAfterHFRIncreaseTrigger t2)
                    {
                        triggertable.Add("HFRTrendPercentage", t2.HFRTrendPercentage);
                        triggertable.Add("OriginalHFR", t2.OriginalHFR);
                        triggertable.Add("SampleSize", t2.SampleSize);
                        triggertable.Add("DeltaHFR", t2.Amount);
                    }
                    else if (trigger is AutofocusAfterTemperatureChangeTrigger t3)
                    {
                        triggertable.Add("DeltaTemperature", t3.DeltaT);
                        triggertable.Add("TargetTemperature", t3.Amount);
                    }
                    else if (trigger is AutofocusAfterTimeTrigger t4)
                    {
                        triggertable.Add("DeltaTime", t4.Amount);
                        triggertable.Add("ElapsedTime", t4.Elapsed);
                    }
                    else if (trigger is DitherAfterExposures t5)
                    {
                        triggertable.Add("Exposures", t5.ProgressExposures);
                        triggertable.Add("TargetExposures", t5.AfterExposures);
                    }
                    else if (trigger is MeridianFlipTrigger t6)
                    {
                        triggertable.Add("TimeToFlip", t6.TimeToMeridianFlip);
                    }
                    else if (trigger is CenterAfterDriftTrigger t7)
                    {
                        triggertable.Add("Coordinates", t7.Coordinates);
                        triggertable.Add("Drift", t7.LastDistanceArcMinutes);
                        triggertable.Add("TargetDrift", t7.DistanceArcMinutes);
                    }
                    triggers.Add(triggertable);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

            }
            return triggers;
        }

        private static List<Hashtable> getConditions(SequenceContainer sequence)
        {
            List<Hashtable> conditions = new List<Hashtable>();
            foreach (var condition in sequence.Conditions)
            {
                try
                {
                    Hashtable ctable = new Hashtable
                    {
                        { "Name", condition.Name + "_Condition" },
                        { "Status", condition.Status.ToString() }
                    };
                    if (condition is TimeCondition c1)
                    {
                        ctable.Add("RemainingTime", c1.RemainingTime);
                        ctable.Add("TargetTime", c1.DateTime.Now + c1.RemainingTime);
                    }
                    else if (condition is LoopForAltitudeBase c2)
                    {
                        ctable.Add("Altitude", c2.Data.Offset);
                        ctable.Add("CurrentAltitude", c2.Data.CurrentAltitude);
                        ctable.Add("ExpectedTime", c2.Data.ExpectedTime);
                        ctable.Add("ExpectedDateTime", c2.Data.ExpectedDateTime);
                    }
                    else if (condition is LoopCondition c3)
                    {
                        ctable.Add("Iterations", c3.Iterations);
                        ctable.Add("CompletedIterations", c3.CompletedIterations);
                    }
                    else if (condition is MoonIlluminationCondition c5)
                    {
                        ctable.Add("TargetIllumination", c5.UserMoonIllumination);
                        ctable.Add("CurrentIllumination", c5.CurrentMoonIllumination);
                    }
                    else if (condition is TimeSpanCondition c6)
                    {
                        ctable.Add("RemainingTime", c6.RemainingTime);
                        ctable.Add("TargetTime", c6.DateTime.Now + c6.RemainingTime);
                    }
                    conditions.Add(ctable);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            return conditions;
        }

        private static List<Hashtable> getSequenceRecursivley(ISequenceContainer sequence)
        {
            List<Hashtable> result = new List<Hashtable>();

            foreach (var item in sequence.Items)
            {
                try
                {
                    Hashtable it = new Hashtable
                {
                    { "Name", item.Name },
                    { "Status", item.Status.ToString() },
                };

                    if (item is ISequenceContainer container && item is not SmartExposure && item is not TakeManyExposures)
                    {
                        it["Name"] = item.Name + "_Container";
                        it.Add("Items", getSequenceRecursivley(container));
                        if (container is SequenceContainer sc)
                        {
                            it.Add("Conditions", getConditions(sc));
                            it.Add("Triggers", getTriggers(sc));
                        }
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
                        it.Add("CompletedIterations", i4.GetLoopCondition().CompletedIterations);
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
#if WINDOWS
                    else if (item is NINA.Sequencer.SequenceItem.Utility.MessageBox i22)
                    {
                        it.Add("Text", i22.Text);
                    }
                    else if (item is ExternalScript i23)
                    {
                        it.Add("Script", i23.Script);
                    }
#endif
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
                        it.Add("TargetTime", i26.DateTime.Now + i26.GetEstimatedDuration());
                        it.Add("CalculatedWaitDuration", i26.GetEstimatedDuration());
                    }
                    else if (item is WaitForTimeSpan i27)
                    {
                        it.Add("Delay", i27.Time);
                    }

                    result.Add(it);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return result;
        }

        [Route(HttpVerbs.Get, "/sequence/start")]
        public void SequenceStart([QueryField] bool skipValidation)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else if (sequence.IsAdvancedSequenceRunning())
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is already running", 409));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => sequence.StartAdvancedSequence(skipValidation));
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
                    Application.Current.Dispatcher.Invoke(sequence.CancelAdvancedSequence);
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

        [Route(HttpVerbs.Get, "/sequence/reset")]
        public void SequenceReset()
        {
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
                    ISequenceRootContainer root = sequence.GetSequenceRoot();
                    Application.Current.Dispatcher.Invoke(root.ResetAll);
                    response.Response = "Sequence reset";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Post, "/sequence/load")]
        public async Task SequenceLoad()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 400));
                }
                else if (sequence.IsAdvancedSequenceRunning())
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is already running", 400));
                }
                else
                {
                    var mediator = (SequenceMediator)sequence;
                    object nav = mediator.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mediator);
                    object factory = nav.GetType().GetField("factory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nav);

                    var converter = new SequenceJsonConverter((ISequencerFactory)factory);

                    ISequenceContainer container = converter.Deserialize(await HttpContext.GetRequestBodyAsStringAsync());

                    Application.Current.Dispatcher.Invoke(() => sequence.SetAdvancedSequence((SequenceRootContainer)container));

                    response.Response = "Sequence updated";
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
        public void GetSequenceLoad([QueryField(true)] string sequenceName)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 400));
                }
                else if (sequence.IsAdvancedSequenceRunning())
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is already running", 400));
                }
                else
                {
                    var mediator = (SequenceMediator)sequence;
                    object nav = mediator.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mediator);
                    ISequencerFactory factory = (ISequencerFactory)nav.GetType().GetField("factory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nav);

                    var converter = new SequenceJsonConverter(factory);

                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;
                    string sequenceFile = Path.Combine(sequenceFolder, sequenceName + ".json");

                    if (!File.Exists(sequenceFile))
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Sequence not found", 400));
                    }
                    else
                    {
                        ISequenceContainer container = converter.Deserialize(File.ReadAllText(sequenceFile));

                        SequenceRootContainer root;

                        if (container is DeepSkyObjectContainer dso)
                        {
                            root = factory.GetContainer<SequenceRootContainer>();
                            root.Name = Loc.Instance["LblAdvancedSequenceTitle"];
                            root.Add(factory.GetContainer<StartAreaContainer>());
                            var target = factory.GetContainer<TargetAreaContainer>();
                            target.Add(dso);
                            root.Add(target);
                            root.Add(factory.GetContainer<EndAreaContainer>());
                        }
                        else
                        {
                            root = (SequenceRootContainer)container;
                        }

                        Application.Current.Dispatcher.Invoke(() => sequence.SetAdvancedSequence(root));

                        response.Response = "Sequence updated";
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

        [Route(HttpVerbs.Get, "/sequence/list-available")]
        public void SequenceGetAvailable()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;

                List<string> f = [];

                List<string> files = FileSystemHelper.GetFilesRecursively(sequenceFolder);
                foreach (string file in files)
                {
                    if (file.EndsWith(".json"))
                    {
                        string cleaned = file.Replace(sequenceFolder, "").Replace("\\", "/").Replace(".json", "");
                        if (cleaned.StartsWith('/'))
                        {
                            cleaned = cleaned[1..];
                        }
                        f.Add(cleaned);
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

        [Route(HttpVerbs.Get, "/sequence/set-target")]
        public void SequenceSetTarget([QueryField] string name, [QueryField] double ra, [QueryField] double dec, [QueryField] double rotation, [QueryField] int index)
        {
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
                    var targets = sequence.GetAllTargetsInAdvancedSequence();
                    if (targets.Count <= index)
                    {
                        response = CoreUtility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE);
                    }
                    else
                    {
                        IDeepSkyObjectContainer container = targets[0];
                        container.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                        container.Target.TargetName = name;
                        container.Target.PositionAngle = rotation;
                        container.Name = name;
                        response.Response = "Target updated";
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
    }
}
