#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.ViewModel.AutoFocus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace ninaAPI.WebService.SET
{
    public class EquipmentController
    {
        private static CancellationTokenSource SequenceToken;
        private static CancellationTokenSource SlewToken;
        private static CancellationTokenSource GuideToken;
        private static CancellationTokenSource AFToken;
        private static CancellationTokenSource DomeToken;

        #region "Old Stuff"
        public static async Task<HttpResponse> Camera(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            ICameraMediator cam = AdvancedAPI.Controls.Camera;

            if (property.Equals("connect"))
            {
                if (!cam.GetInfo().Connected)
                {
                    await cam.Rescan();
                    await cam.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (cam.GetInfo().Connected)
                {
                    await cam.Disconnect();
                }
                return response;
            }
            if (property.Equals("abort-exposure"))
            {
                cam.AbortExposure();
            }
            return response;
        }

        public static async Task<HttpResponse> Telescope(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;

            if (property.Equals("connect"))
            {
                if (!telescope.GetInfo().Connected)
                {
                    await telescope.Rescan();
                    await telescope.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (telescope.GetInfo().Connected)
                {
                    await telescope.Disconnect();
                }
                return response;
            }
            if (property.Equals("park"))
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
            if (property.Equals("unpark"))
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

        public static async Task<HttpResponse> Focuser(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;

            if (property.Equals("connect"))
            {
                if (!focuser.GetInfo().Connected)
                {
                    await focuser.Rescan();
                    await focuser.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (focuser.GetInfo().Connected)
                {
                    await focuser.Disconnect();
                }
                return response;
            }
            if (property.Equals("auto-focus"))
            {
                if (AFToken != null)
                {
                    AFToken.Cancel();
                }
                AdvancedAPI.Controls.AutoFocusFactory.Create().StartAutoFocus(AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter, AFToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                response.Response = "AutoFocus in progress";
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Rotator(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

            if (property.Equals("connect"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    await rotator.Rescan();
                    await rotator.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (rotator.GetInfo().Connected)
                {
                    await rotator.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> FilterWheel(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

            if (property.Equals("connect"))
            {
                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    await filterwheel.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Dome(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            IDomeMediator dome = AdvancedAPI.Controls.Dome;

            if (property.Equals("connect"))
            {
                if (!dome.GetInfo().Connected)
                {
                    await dome.Rescan();
                    await dome.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (dome.GetInfo().Connected)
                {
                    await dome.Disconnect();
                }
                return response;
            }
            if (property.Equals("open"))
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
            if (property.Equals("close"))
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
            if (property.Equals("stop"))
            {
                DomeToken?.Cancel();
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Switch(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            ISwitchMediator switches = AdvancedAPI.Controls.Switch;

            if (property.Equals("connect"))
            {
                if (!switches.GetInfo().Connected)
                {
                    await switches.Rescan();
                    await switches.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (switches.GetInfo().Connected)
                {
                    await switches.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Guider(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            IGuiderMediator guider = AdvancedAPI.Controls.Guider;

            if (property.Equals("connect"))
            {
                if (!guider.GetInfo().Connected)
                {
                    await guider.Rescan();
                    await guider.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.Disconnect();
                }
                return response;
            }
            if (property.Equals("start"))
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
            if (property.Equals("stop"))
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

        public static async Task<HttpResponse> FlatDevice(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

            if (property.Equals("connect"))
            {
                if (!flat.GetInfo().Connected)
                {
                    await flat.Rescan();
                    await flat.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (flat.GetInfo().Connected)
                {
                    await flat.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> SafteyMonitor(string property, string value)
        {
            HttpResponse response = new HttpResponse();
            ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;

            if (property.Equals("connect"))
            {
                if (!safety.GetInfo().Connected)
                {
                    await safety.Rescan();
                    await safety.Connect();
                }
                return response;
            }
            if (property.Equals("disconnect"))
            {
                if (safety.GetInfo().Connected)
                {
                    await safety.Disconnect();
                }
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Sequence(string action, string value)
        {
            HttpResponse response = new HttpResponse();
            ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

            action = action.ToLower();

            if (action.Equals("start"))
            {
                SequenceToken = new CancellationTokenSource();
                sequence.GetAllTargets()[0].Parent.Parent.Run(AdvancedAPI.Controls.StatusMediator.GetStatus(), SequenceToken.Token);
                response.Response = "Sequence in progress";
                return response;
            }
            else if (action.Equals("stop"))
            {
                SequenceToken?.Cancel();
                return response;
            }
            return response;
        }

        public static async Task<HttpResponse> Application(string action, string value)
        {
            HttpResponse response = new HttpResponse();

            action = action.ToLower();

            if (action.Equals("screenshot")) // Captures a screenshot and returns it base64 encoded
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
            if (action.Equals("switch"))
            {
                switch (value)
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
                }
            }
            return response;
        }
        #endregion

        public static HttpResponse ChangeProfileValue(string path, string value)
        {
            HttpResponse response = new HttpResponse();
            string[] pathSplit = path.Split('-'); // CameraSettings, PixelSize
            object position = AdvancedAPI.Controls.Profile.ActiveProfile;
            if (pathSplit.Length == 0)
            {
                position.GetType().GetProperty(path).SetValue(position, value);
                return response;
            }
            for (int i = 0; i <= pathSplit.Length - 2; i++)
            {
                position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
            }
            PropertyInfo prop = position.GetType().GetProperty(pathSplit[pathSplit.Length - 1]);
            prop.SetValue(position, value.CastString(prop.PropertyType));
            return response;
        }

        public static HttpResponse SwitchProfile(string id)
        {
            HttpResponse response = new HttpResponse();
            Guid guid = Guid.Parse(id);
            ProfileMeta profile = AdvancedAPI.Controls.Profile.Profiles.Where(x => x.Id == guid).FirstOrDefault();
            AdvancedAPI.Server.SyncContext.Send(x => AdvancedAPI.Controls.Profile.SelectProfile(profile), null);
            return response;
        }
    }
}
