#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Interfaces.Mediator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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


        public static async Task<Hashtable> Camera(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            ICameraMediator cam = AdvancedAPI.Controls.Camera;

            if (property.Equals("connect"))
            {
                if (!cam.GetInfo().Connected)
                {
                    await cam.Rescan();
                    result["Success"] = await cam.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (cam.GetInfo().Connected)
                {
                    await cam.Disconnect();
                    result["Success"] = !cam.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("abortexposure")) 
            {
                cam.AbortExposure();
                result["Success"] = true;
                return result;
            }
            return result;
        }

        public static async Task<Hashtable> Telescope(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;

            if (property.Equals("connect"))
            {
                if (!telescope.GetInfo().Connected)
                {
                    await telescope.Rescan();
                    result["Success"] = await telescope.Connect();
                    return result;
                }
            }
            if (property.Equals("disconnect"))
            {
                if (telescope.GetInfo().Connected)
                {
                    await telescope.Disconnect();
                    result["Success"] = !telescope.GetInfo().Connected;
                    return result;
                }
            }
            if (property.Equals("park"))
            {
                if (telescope.GetInfo().Slewing)
                {
                    result["ErrorMessage"] = "Telescope is slewing";
                    return result;
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                telescope.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                result["Success"] = true;
                return result;
            }
            if (property.Equals("unpark"))
            {
                if (!telescope.GetInfo().AtPark)
                {
                    result["Success"] = true;
                    return result;
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                telescope.UnparkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                result["Success"] = true;
                return result;
            }
            return result;
        }

        public static async Task<Hashtable> Focuser(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;

            if (property.Equals("connect"))
            {
                if (!focuser.GetInfo().Connected)
                {
                    await focuser.Rescan();
                    result["Success"] = await focuser.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (focuser.GetInfo().Connected)
                {
                    await focuser.Disconnect();
                    result["Success"] = !focuser.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            return result;
        }

        public static async Task<Hashtable> Rotator(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

            if (property.Equals("connect"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    await rotator.Rescan();
                    result["Success"] = await rotator.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (rotator.GetInfo().Connected)
                {
                    await rotator.Disconnect();
                    result["Success"] = !rotator.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            return result;
        }

        public static async Task<Hashtable> FilterWheel(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

            if (property.Equals("connect"))
            {
                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    result["Success"] = await filterwheel.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                    result["Success"] = !filterwheel.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            return result;
        }

        public static async Task<Hashtable> Dome(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            IDomeMediator dome = AdvancedAPI.Controls.Dome;

            if (property.Equals("connect"))
            {
                if (!dome.GetInfo().Connected)
                {
                    await dome.Rescan();
                    result["Success"] = await dome.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (dome.GetInfo().Connected)
                {
                    await dome.Disconnect();
                    result["Success"] = !dome.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            return result;
        }

        public static async Task<Hashtable> Switch(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            ISwitchMediator switches = AdvancedAPI.Controls.Switch;

            if (property.Equals("connect"))
            {
                if (!switches.GetInfo().Connected)
                {
                    await switches.Rescan();
                    result["Success"] = await switches.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (switches.GetInfo().Connected)
                {
                    await switches.Disconnect();
                    result["Success"] = !switches.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            return result;
        }

        public static async Task<Hashtable> Guider(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            IGuiderMediator guider = AdvancedAPI.Controls.Guider;

            if (property.Equals("connect"))
            {
                if (!guider.GetInfo().Connected)
                {
                    await guider.Rescan();
                    result["Success"] = await guider.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.Disconnect();
                    result["Success"] = !guider.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("start")) 
            {
                if (guider.GetInfo().Connected)
                {
                    GuideToken?.Cancel();
                    GuideToken = new CancellationTokenSource();
                    result["Success"] = await guider.StartGuiding(false, AdvancedAPI.Controls.StatusMediator.GetStatus(), GuideToken.Token);
                    return result;
                }
                result["ErrorMessage"] = "Guider is not connected";
                return result;
            }
            if (property.Equals("stop"))
            {
                if (guider.GetInfo().Connected)
                {
                    result["Success"] = await guider.StopGuiding(GuideToken.Token);
                    return result;
                }
                result["ErrorMessage"] = "Guider is not connected";
                return result;
            }
            return result;
        }

        public static async Task<Hashtable> FlatDevice(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

            if (property.Equals("connect"))
            {
                if (!flat.GetInfo().Connected)
                {
                    await flat.Rescan();
                    result["Success"] = await flat.Connect();
                    return result;
                }
                result["Success"] = true;
            }
            if (property.Equals("disconnect"))
            {
                if (flat.GetInfo().Connected)
                {
                    await flat.Disconnect();
                    result["Success"] = !flat.GetInfo().Connected;
                    return result;
                }
                result["Success"] = true;
            }
            return result;
        }

        public static async Task<Hashtable> SafteyMonitor(string property)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;

            if (property.Equals("connect"))
            {
                if (!safety.GetInfo().Connected)
                {
                    await safety.Rescan();
                    result["Success"] = await safety.Connect();
                    return result;
                }
            }
            if (property.Equals("disconnect"))
            {
                if (safety.GetInfo().Connected)
                {
                    await safety.Disconnect();
                    result["Success"] = !safety.GetInfo().Connected;
                    return result;
                }
            }
            return result;
        }

        public static async Task<Hashtable> Sequence(string action)
        {
            Hashtable result = new Hashtable();
            result.Add("Success", false);
            ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

            action = action.ToLower();

            if (action.Equals("start"))
            {
                SequenceToken = new CancellationTokenSource();
                sequence.GetAllTargets()[0].Parent.Parent.Run(AdvancedAPI.Controls.StatusMediator.GetStatus(), SequenceToken.Token);
                result["Success"] = true;
                return result;
            }
            else if (action.Equals("stop"))
            {
                SequenceToken?.Cancel();
                result["Success"] = true;
                return result;
            }
            return result;
        }

        public static async Task<Hashtable> Application(string action)
        {
            Hashtable result = new Hashtable();
            result["Success"] = false;

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
                    Bitmap map = new Bitmap(bmpScreenCapture);
                    using (MemoryStream memory = new MemoryStream())
                    {
                        
                        map.Save(memory, ImageFormat.Jpeg);
                        result["Image"] = Convert.ToBase64String(memory.ToArray());
                    }
                }
                result["Success"] = true;
                return result;
            }
            switch (action)
            {
                case "switch-equipment":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.EQUIPMENT);
                    result["Success"] = true;
                    return result;
                case "switch-skyatlas":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SKYATLAS);
                    result["Success"] = true;
                    return result;
                case "switch-framing":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FRAMINGASSISTANT);
                    result["Success"] = true;
                    return result;
                case "switch-flatwizard":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FLATWIZARD);
                    result["Success"] = true;
                    return result;
                case "switch-sequencer":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SEQUENCE);
                    result["Success"] = true;
                    return result;
                case "switch-imaging":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.IMAGING);
                    result["Success"] = true;
                    return result;
                case "switch-options":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.OPTIONS);
                    result["Success"] = true;
                    return result;
            }
            return result;
        }
    }
}
