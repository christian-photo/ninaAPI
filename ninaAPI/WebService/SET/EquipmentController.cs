#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Sequencer.Interfaces.Mediator;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ninaAPI.WebService.SET
{
    public class EquipmentController
    {
        private static CancellationTokenSource SequenceToken;
        private static CancellationTokenSource SlewToken;
        private static CancellationTokenSource GuideToken;
        private static CancellationTokenSource AFToken;
        private static CancellationTokenSource DomeToken;

        public static async Task<HttpResponse> Camera(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            ICameraMediator cam = AdvancedAPI.Controls.Camera;

            if (data.Action.Equals("connect"))
            {
                if (!cam.GetInfo().Connected)
                {
                    await cam.Rescan();
                    await cam.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (cam.GetInfo().Connected)
                {
                    await cam.Disconnect();
                }
                return response;
            }
            if (data.Action.Equals("abort-exposure"))
            {
                cam.AbortExposure();
            }
            return response;
        }

        public static async Task<HttpResponse> Telescope(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;

            if (data.Action.Equals("connect"))
            {
                if (!telescope.GetInfo().Connected)
                {
                    await telescope.Rescan();
                    await telescope.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (telescope.GetInfo().Connected)
                {
                    await telescope.Disconnect();
                }
                return response;
            }
            if (data.Action.Equals("park"))
            {
                if (telescope.GetInfo().Slewing)
                {
                    telescope.StopSlew();
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                telescope.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                response.Response = "Park in progress";
                return response;
            }
            if (data.Action.Equals("unpark"))
            {
                if (!telescope.GetInfo().AtPark)
                {
                    return response;
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                telescope.UnparkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                response.Response = "Unpark in progress";
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Focuser(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;

            if (data.Action.Equals("connect"))
            {
                if (!focuser.GetInfo().Connected)
                {
                    await focuser.Rescan();
                    await focuser.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (focuser.GetInfo().Connected)
                {
                    await focuser.Disconnect();
                }
                return response;
            }
            if (data.Action.Equals("auto-focus"))
            {
                if (AFToken != null)
                {
                    AFToken.Cancel();
                }
                AFToken = new CancellationTokenSource();
                AdvancedAPI.Controls.AutoFocusFactory.Create().StartAutoFocus(AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter, AFToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                response.Response = "AutoFocus in progress";
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Rotator(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

            if (data.Action.Equals("connect"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    await rotator.Rescan();
                    await rotator.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (rotator.GetInfo().Connected)
                {
                    await rotator.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> FilterWheel(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

            if (data.Action.Equals("connect"))
            {
                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    await filterwheel.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Dome(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            IDomeMediator dome = AdvancedAPI.Controls.Dome;

            if (data.Action.Equals("connect"))
            {
                if (!dome.GetInfo().Connected)
                {
                    await dome.Rescan();
                    await dome.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (dome.GetInfo().Connected)
                {
                    await dome.Disconnect();
                }
                return response;
            }
            if (data.Action.Equals("open"))
            {
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterOpen || dome.GetInfo().ShutterStatus == ShutterState.ShutterOpening)
                {
                    response.Response = "Shutter already open";
                    return response;
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.OpenShutter(DomeToken.Token);
                return response;
            }
            if (data.Action.Equals("close"))
            {
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterClosed || dome.GetInfo().ShutterStatus == ShutterState.ShutterClosing)
                {
                    response.Response = "Shutter already closed";
                    return response;
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.CloseShutter(DomeToken.Token);
                return response;
            }
            if (data.Action.Equals("stop"))
            {
                DomeToken?.Cancel();
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Switch(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            ISwitchMediator switches = AdvancedAPI.Controls.Switch;

            if (data.Action.Equals("connect"))
            {
                if (!switches.GetInfo().Connected)
                {
                    await switches.Rescan();
                    await switches.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (switches.GetInfo().Connected)
                {
                    await switches.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Guider(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            IGuiderMediator guider = AdvancedAPI.Controls.Guider;

            if (data.Action.Equals("connect"))
            {
                if (!guider.GetInfo().Connected)
                {
                    await guider.Rescan();
                    await guider.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.Disconnect();
                }
                return response;
            }
            if (data.Action.Equals("start"))
            {
                if (guider.GetInfo().Connected)
                {
                    GuideToken?.Cancel();
                    GuideToken = new CancellationTokenSource();
                    await guider.StartGuiding(false, AdvancedAPI.Controls.StatusMediator.GetStatus(), GuideToken.Token);
                    return response;
                }
                return Utility.CreateErrorTable("Guider not connected");
            }
            if (data.Action.Equals("stop"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.StopGuiding(GuideToken.Token);
                    return response;
                }
                return Utility.CreateErrorTable("Guider not connected");
            }
            return response;
        }

        public static async Task<HttpResponse> FlatDevice(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

            if (data.Action.Equals("connect"))
            {
                if (!flat.GetInfo().Connected)
                {
                    await flat.Rescan();
                    await flat.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (flat.GetInfo().Connected)
                {
                    await flat.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> SafetyMonitor(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;

            if (data.Action.Equals("connect"))
            {
                if (!safety.GetInfo().Connected)
                {
                    await safety.Rescan();
                    await safety.Connect();
                }
                return response;
            }
            if (data.Action.Equals("disconnect"))
            {
                if (safety.GetInfo().Connected)
                {
                    await safety.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Sequence(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

            if (data.Action.Equals("start"))
            {
                SequenceToken = new CancellationTokenSource();
                sequence.GetAllTargets()[0].Parent.Parent.Run(AdvancedAPI.Controls.StatusMediator.GetStatus(), SequenceToken.Token);
                response.Response = "Sequence in progress";
                return response;
            }
            else if (data.Action.Equals("stop"))
            {
                SequenceToken?.Cancel();
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Application(POSTData data)
        {
            HttpResponse response = new HttpResponse();

            if (data.Action.Equals("screenshot")) // Captures a screenshot and returns it base64 encoded
            {
                using (Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                            Screen.PrimaryScreen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                    {
                        g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                         Screen.PrimaryScreen.Bounds.Y,
                                         0, 0,
                                         bmpScreenCapture.Size,
                                         CopyPixelOperation.SourceCopy);
                    }

                    response.Response = Utility.BitmapToBase64(bmpScreenCapture);
                }
                return response;
            }
            if (data.Action.Equals("switch"))
            {
                switch (data.Parameter[0])
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
                        return Utility.CreateErrorTable("Invalid parameter");
                }
            }
            return response;
        }

        public static HttpResponse ChangeProfileValue(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            if (string.IsNullOrEmpty(data.Action))
                return Utility.CreateErrorTable("Invalid Path");
            
            string[] pathSplit = (data.Action).Split('-'); // CameraSettings, PixelSize
            object position = AdvancedAPI.Controls.Profile.ActiveProfile;
            if (pathSplit.Length == 0)
            {
                position.GetType().GetProperty((string)data.Parameter[0]).SetValue(position, data.Parameter[0]);
                return response;
            }
            for (int i = 0; i <= pathSplit.Length - 2; i++)
            {
                position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
            }
            PropertyInfo prop = position.GetType().GetProperty(pathSplit[pathSplit.Length - 1]);
            prop.SetValue(position, ((string)data.Parameter[0]).CastString(prop.PropertyType));
            return response;
        }

        public static HttpResponse SwitchProfile(POSTData data)
        {
            HttpResponse response = new HttpResponse();
            Guid guid = Guid.Parse(data.Action);
            ProfileMeta profile = AdvancedAPI.Controls.Profile.Profiles.Where(x => x.Id == guid).FirstOrDefault();
            AdvancedAPI.Controls.Profile.SelectProfile(profile);
            return response;
        }
    }
}
