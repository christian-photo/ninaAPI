#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ninaAPI.WebService.V2
{
    public class CaptureParameter
    {
        public bool solve = false;
        public bool getResult = false;
        public bool resize = false;

        public double duration = 0;

        public int quality = 0;

        public Size size = Size.Empty;
    }

    public class EquipmentControllerV2
    {
        private static CancellationTokenSource SlewToken;
        private static CancellationTokenSource GuideToken;
        private static CancellationTokenSource AFToken;
        private static CancellationTokenSource DomeToken;
        private static CancellationTokenSource RotatorToken;
        private static CancellationTokenSource CaptureToken;

        private static PlateSolveResult plateSolveResult;
        private static IRenderedImage renderedImage;
        private static Task CaptureTask;

        public static async Task<HttpResponse> Camera(string action, CaptureParameter captureParameter)
        {
            HttpResponse response = new HttpResponse();
            ICameraMediator cam = AdvancedAPI.Controls.Camera;

            if (action.Equals("connect"))
            {
                if (!cam.GetInfo().Connected)
                {
                    await cam.Rescan();
                    await cam.Connect();
                }
                response.Response = "Camera connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (cam.GetInfo().Connected)
                {
                    await cam.Disconnect();
                }
                response.Response = "Camera disconnected";
                return response;
            }
            else if (action.Equals("abort-exposure"))
            {
                if (!cam.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (cam.GetInfo().IsExposing)
                {
                    response.Response = "Exposure aborted";
                    cam.AbortExposure();
                }
                else
                {
                    response.Response = "Camera not exposing";
                }
                return response;
            }
            else if (action.Equals("capture"))
            {
                if (!cam.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Camera not connected", 409));
                }
                else if (cam.GetInfo().IsExposing)
                {
                    return Utility.CreateErrorTable(new Error("Camera currently exposing", 409));
                }

                if (CaptureTask != null && !captureParameter.getResult)
                {
                    if (!CaptureTask.IsCompleted)
                    {
                        response.Response = "Capture already in progress";
                    }
                }
                if (CaptureTask != null && captureParameter.getResult)
                {
                    if (CaptureTask.Status == TaskStatus.RanToCompletion)
                    {
                        Hashtable result = new Hashtable();
                        result.Add("Image", EquipmentMediatorV2.ResizeAndConvertBitmap(renderedImage.Image, captureParameter.size, captureParameter.quality));
                        if (plateSolveResult != null)
                            result.Add("Platesolve", plateSolveResult);

                        response.Response = result;
                    }
                    else
                    {
                        response.Response = CaptureTask.Status.ToString();
                    }
                }
                else if (CaptureTask is null && captureParameter.getResult)
                {
                    response.Response = "No capture processed";
                }
                else
                {
                    CaptureToken?.Cancel();
                    CaptureToken = new CancellationTokenSource();

                    CaptureTask = Task.Run(async () =>
                    {
                        renderedImage = null;
                        plateSolveResult = null;
                        IPlateSolveSettings settings = AdvancedAPI.Controls.Profile.ActiveProfile.PlateSolveSettings;

                        CaptureSequence sequence = new CaptureSequence(
                            captureParameter.duration <= 0 ? settings.ExposureTime : captureParameter.duration,
                            CaptureSequence.ImageTypes.SNAPSHOT,
                            AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                            new BinningMode(cam.GetInfo().BinX, cam.GetInfo().BinY),
                            1);

                        PrepareImageParameters parameters = new PrepareImageParameters(autoStretch: true);
                        IExposureData exposure = await AdvancedAPI.Controls.Imaging.CaptureImage(sequence, CaptureToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                        AdvancedAPI.Controls.ImageHistory.Add(exposure.MetaData.Image.Id, "SNAPSHOT");
                        renderedImage = await AdvancedAPI.Controls.Imaging.PrepareImage(exposure, parameters, CaptureToken.Token);


                        if (captureParameter.solve)
                        {
                            IPlateSolverFactory platesolver = AdvancedAPI.Controls.PlateSolver;
                            Coordinates coordinates = AdvancedAPI.Controls.Mount.GetCurrentPosition();
                            double focalLength = AdvancedAPI.Controls.Profile.ActiveProfile.TelescopeSettings.FocalLength;
                            double pixelSize = cam.GetInfo().PixelSize;
                            CaptureSolverParameter solverParameter = new CaptureSolverParameter()
                            {
                                Attempts = 1,
                                Binning = settings.Binning,
                                BlindFailoverEnabled = settings.BlindFailoverEnabled,
                                Coordinates = coordinates,
                                DownSampleFactor = settings.DownSampleFactor,
                                FocalLength = focalLength,
                                MaxObjects = settings.MaxObjects,
                                Regions = settings.Regions,
                                SearchRadius = settings.SearchRadius,
                                PixelSize = cam.GetInfo().PixelSize
                            };
                            IImageSolver captureSolver = platesolver.GetImageSolver(platesolver.GetPlateSolver(settings), platesolver.GetBlindSolver(settings));
                            plateSolveResult = await captureSolver.Solve(renderedImage.RawImageData, solverParameter, AdvancedAPI.Controls.StatusMediator.GetStatus(), CaptureToken.Token);
                        }
                    }, CaptureToken.Token);

                    response.Response = "Capture started";
                }

                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Mount(string action)
        {
            HttpResponse response = new HttpResponse();
            ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

            if (action.Equals("connect"))
            {
                if (!mount.GetInfo().Connected)
                {
                    await mount.Rescan();
                    await mount.Connect();
                }
                response.Response = "Mount connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (mount.GetInfo().Connected)
                {
                    await mount.Disconnect();
                }
                response.Response = "Mount disconnected";
                return response;
            }
            else if (action.Equals("park"))
            {
                if (!mount.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                if (mount.GetInfo().AtPark)
                {
                    response.Response = "Mount already parked";
                    return response;
                }
                if (mount.GetInfo().Slewing)
                {
                    mount.StopSlew();
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                mount.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                response.Response = "Parking";
                return response;
            }
            else if (action.Equals("unpark"))
            {
                if (!mount.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                if (!mount.GetInfo().AtPark)
                {
                    response.Response = "Mount not parked";
                    return response;
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                mount.UnparkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                response.Response = "Unparking";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Focuser(string action)
        {
            HttpResponse response = new HttpResponse();
            IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;

            if (action.Equals("connect"))
            {
                if (!focuser.GetInfo().Connected)
                {
                    await focuser.Rescan();
                    await focuser.Connect();
                }
                response.Response = "Focuser connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (focuser.GetInfo().Connected)
                {
                    await focuser.Disconnect();
                }
                response.Response = "Focuser disconnected";
                return response;
            }
            else if (action.Equals("auto-focus"))
            {
                if (!focuser.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Focuser not connected", 409));
                }
                AFToken?.Cancel();
                AFToken = new CancellationTokenSource();
                AdvancedAPI.Controls.AutoFocusFactory.Create().StartAutoFocus(AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter, AFToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                response.Response = "Autofocus started";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Rotator(string action, float position)
        {
            HttpResponse response = new HttpResponse();
            IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;


            if (action.Equals("connect"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    await rotator.Rescan();
                    await rotator.Connect();
                }
                response.Response = "Rotator connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (rotator.GetInfo().Connected)
                {
                    await rotator.Disconnect();
                }
                response.Response = "Rotator disconnected";
                return response;
            }
            else if (action.Equals("move"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                RotatorToken?.Cancel();
                RotatorToken = new CancellationTokenSource();
                rotator.Move(position, RotatorToken.Token);
                response.Response = "Rotator move started";
                return response;
            }
            else if (action.Equals("move-mechanical"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                RotatorToken?.Cancel();
                RotatorToken = new CancellationTokenSource();
                rotator.MoveMechanical(position, RotatorToken.Token);
                response.Response = "Rotator move started";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> FilterWheel(string action)
        {
            HttpResponse response = new HttpResponse();
            IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

            // TODO: Implement change filter

            if (action.Equals("connect"))
            {
                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    await filterwheel.Connect();
                }
                response.Response = "Filterwheel connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                }
                response.Response = "Filterwheel disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Dome(string action)
        {
            HttpResponse response = new HttpResponse();
            IDomeMediator dome = AdvancedAPI.Controls.Dome;

            if (action.Equals("connect"))
            {
                if (!dome.GetInfo().Connected)
                {
                    await dome.Rescan();
                    await dome.Connect();
                }
                response.Response = "Dome connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (dome.GetInfo().Connected)
                {
                    await dome.Disconnect();
                }
                response.Response = "Dome disconnected";
                return response;
            }
            else if (action.Equals("open"))
            {
                if (!dome.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterOpen || dome.GetInfo().ShutterStatus == ShutterState.ShutterOpening)
                {
                    response.Response = "Shutter already open";
                    return response;
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.OpenShutter(DomeToken.Token);
                response.Response = "Shutter opening";
                return response;
            }
            else if (action.Equals("close"))
            {
                if (!dome.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterClosed || dome.GetInfo().ShutterStatus == ShutterState.ShutterClosing)
                {
                    response.Response = "Shutter already closed";
                    return response;
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.CloseShutter(DomeToken.Token);
                response.Response = "Shutter closing";
                return response;
            }
            else if (action.Equals("stop")) // Can only stop movement that was started by the api
            {
                if (!dome.GetInfo().Connected)
                {
                    return Utility.CreateErrorTable(new Error("Dome not connected", 409));
                }
                DomeToken?.Cancel();
                response.Response = "Movement stopped";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Switch(string action)
        {
            HttpResponse response = new HttpResponse();
            ISwitchMediator switches = AdvancedAPI.Controls.Switch;

            if (action.Equals("connect"))
            {
                if (!switches.GetInfo().Connected)
                {
                    await switches.Rescan();
                    await switches.Connect();
                }
                response.Response = "Switch connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (switches.GetInfo().Connected)
                {
                    await switches.Disconnect();
                }
                response.Response = "Switch disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Guider(string action)
        {
            HttpResponse response = new HttpResponse();
            IGuiderMediator guider = AdvancedAPI.Controls.Guider;

            if (action.Equals("connect"))
            {
                if (!guider.GetInfo().Connected)
                {
                    await guider.Rescan();
                    await guider.Connect();
                }
                response.Response = "Guider connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.Disconnect();
                }
                response.Response = "Guider disconnected";
                return response;
            }
            else if (action.Equals("start"))
            {
                if (guider.GetInfo().Connected)
                {
                    GuideToken?.Cancel();
                    GuideToken = new CancellationTokenSource();
                    await guider.StartGuiding(false, AdvancedAPI.Controls.StatusMediator.GetStatus(), GuideToken.Token);
                    response.Response = "Guiding started";
                    return response;
                }
                return Utility.CreateErrorTable(new Error("Guider not connected", 409));
            }
            else if (action.Equals("stop"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.StopGuiding(GuideToken.Token);
                    response.Response = "Guiding stopped";
                    return response;
                }
                return Utility.CreateErrorTable(new Error("Guider not connected", 409));
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> FlatDevice(string action)
        {
            HttpResponse response = new HttpResponse();
            IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

            if (action.Equals("connect"))
            {
                if (!flat.GetInfo().Connected)
                {
                    await flat.Rescan();
                    await flat.Connect();
                }
                response.Response = "Flatdevice connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (flat.GetInfo().Connected)
                {
                    await flat.Disconnect();
                }
                response.Response = "Flatdevice disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> SafetyMonitor(string action)
        {
            HttpResponse response = new HttpResponse();
            ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;

            if (action.Equals("connect"))
            {
                if (!safety.GetInfo().Connected)
                {
                    await safety.Rescan();
                    await safety.Connect();
                }
                response.Response = "Safetymonitor connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (safety.GetInfo().Connected)
                {
                    await safety.Disconnect();
                }
                response.Response = "Safetymonitor disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Weather(string action)
        {
            HttpResponse response = new HttpResponse();
            IWeatherDataMediator weather = AdvancedAPI.Controls.Weather;

            if (action.Equals("connect"))
            {
                if (!weather.GetInfo().Connected)
                {
                    await weather.Rescan();
                    await weather.Connect();
                }
                response.Response = "Weather connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (weather.GetInfo().Connected)
                {
                    await weather.Disconnect();
                }
                response.Response = "Weather disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static HttpResponse Sequence(string action, bool skipValidation, string sequenceName = "", string sequenceJson = "")
        {
            HttpResponse response = new HttpResponse();
            ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

            if (action.Equals("start"))
            {
                if (!sequence.Initialized)
                {
                    return Utility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                sequence.StartAdvancedSequence(skipValidation);
                response.Response = "Sequence started";
                return response;
            }
            else if (action.Equals("stop")) // Can only stop the sequence if it was started by the api
            {
                sequence.CancelAdvancedSequence();
                response.Response = "Sequence stopped";
                return response;
            }
            else if (action.Equals("load"))
            {
                if (!string.IsNullOrWhiteSpace(sequenceName))
                {
                    IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                    string file = Path.Combine(profile.SequenceSettings.DefaultSequenceFolder, sequenceName + ".json");
                    sequenceJson = File.ReadAllText(file);
                }
                ISequenceRootContainer container = JsonConvert.DeserializeObject<SequenceRootContainer>(sequenceJson); // Not working
                sequence.SetAdvancedSequence(container);

                response.Response = "Sequence updated";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static HttpResponse Application(string applicationTab)
        {
            HttpResponse response = new HttpResponse();

            response.Response = "Switched tab";
            switch (applicationTab)
            {
                case "equipment":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.EQUIPMENT);
                    return response;
                case "skyatlas":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SKYATLAS);
                    return response;
                case "framing":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FRAMINGASSISTANT);
                    return response;
                case "flatwizard":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FLATWIZARD);
                    return response;
                case "sequencer":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SEQUENCE);
                    return response;
                case "imaging":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.IMAGING);
                    return response;
                case "options":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.OPTIONS);
                    return response;
                default:
                    return Utility.CreateErrorTable(new Error("Invalid application tab", 400));
            }
        }

        public static HttpResponse ChangeProfileValue(string settingPath, object newValue)
        {
            HttpResponse response = new HttpResponse();
            if (string.IsNullOrEmpty(settingPath))
            {
                return Utility.CreateErrorTable(new Error("Invalid path", 400));
            }
            if (newValue is null)
            {
                return Utility.CreateErrorTable(new Error("New value can't be null", 400));
            }

            string[] pathSplit = settingPath.Split('-'); // e.g. 'CameraSettings-PixelSize' -> CameraSettings, PixelSize
            object position = AdvancedAPI.Controls.Profile.ActiveProfile;

            response.Response = "Updated setting";

            if (pathSplit.Length == 1)
            {
                position.GetType().GetProperty(settingPath).SetValue(position, newValue);
                return response;
            }
            for (int i = 0; i <= pathSplit.Length - 2; i++)
            {
                position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
            }
            PropertyInfo prop = position.GetType().GetProperty(pathSplit[^1]);
            prop.SetValue(position, ((string)newValue).CastString(prop.PropertyType));
            return response;
        }

        public static HttpResponse SwitchProfile(string profileID)
        {
            HttpResponse response = new HttpResponse();
            Guid guid = Guid.Parse(profileID);
            IEnumerable<ProfileMeta> x = AdvancedAPI.Controls.Profile.Profiles.Where(x => x.Id == guid);
            if (x.Any())
            {
                ProfileMeta profile = x.First();
                AdvancedAPI.Controls.Profile.SelectProfile(profile);
                response.Response = "Successfully switched profile";
            }
            else
            {
                response = Utility.CreateErrorTable(new Error("No profile with specified id found!", 400));
            }
            return response;
        }
    }
}
