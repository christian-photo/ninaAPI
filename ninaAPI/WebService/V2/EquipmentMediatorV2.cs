#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ninaAPI.WebService.V2
{
    public class EquipmentMediatorV2
    {
        public static HttpResponse GetDeviceInfo(EquipmentType deviceType)
        {
            HttpResponse response = new HttpResponse();
            switch (deviceType)
            {
                case EquipmentType.Camera:
                    ICameraMediator cam = AdvancedAPI.Controls.Camera;
                    response.Response = cam.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Focuser:
                    IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                    response.Response = focuser.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.FlatDevice:
                    IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;
                    response.Response = flat.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Dome:
                    IDomeMediator dome = AdvancedAPI.Controls.Dome;
                    response.Response = dome.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.FilterWheel:
                    IFilterWheelMediator filter = AdvancedAPI.Controls.FilterWheel;
                    response.Response = filter.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Switch:
                    ISwitchMediator switches = AdvancedAPI.Controls.Switch;
                    response.Response = switches.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Guider:
                    IGuiderMediator guider = AdvancedAPI.Controls.Guider;
                    response.Response = guider.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Rotator:
                    IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;
                    response.Response = rotator.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.SafetyMonitor:
                    ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;
                    response.Response = safety.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Mount:
                    ITelescopeMediator telescope = AdvancedAPI.Controls.Mount;
                    response.Response = telescope.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Weather:
                    IWeatherDataMediator weather = AdvancedAPI.Controls.Weather;
                    response.Response = weather.GetInfo().GetAllProperties();
                    return response;

                default:
                    response.Response = "";
                    response.Error = "Invalid device type";
                    response.Success = false;
                    return response;
            }
        }

        public static HttpResponse GetImageHistory(int index)
        {
            List<object> result = new List<object>();
            if (index < 0)
            {
                foreach (HttpResponse response in WebSocketV2.Images)
                {
                    result.Add(((Dictionary<string, object>)response.Response)["ImageStatistics"]);
                }
            }
            else if (index >= 0 && index < WebSocketV2.Images.Count)
            {
                result.Add(((Dictionary<string, object>)WebSocketV2.Images[index].Response)["ImageStatistics"]);
            }
            else if (index >= WebSocketV2.Images.Count)
            {
                return Utility.CreateErrorTable(CommonErrors.INDEX_OUT_OF_RANGE.message);
            }
            return new HttpResponse() { Response = result };
        }

        public static HttpResponse GetImageHistoryCount()
        {
            HttpResponse response = new HttpResponse();
            response.Response = WebSocketV2.Images.Count;
            return response;
        }

        public static HttpResponse GetProfile(bool active)
        {
            try
            {
                List<Hashtable> result = new List<Hashtable>();
                IProfileService profileService = AdvancedAPI.Controls.Profile;

                if (!active)
                {
                    foreach (ProfileMeta profile in profileService.Profiles)
                    {
                        Hashtable table = new Hashtable()
                        {
                            { "Name", profile.Name },
                            { "Id", profile.Id },
                            { "Description", profile.Description },
                            { "LastUsed", profile.LastUsed },
                            { "Location", profile.Location },
                            { "IsActive", profile.IsActive }
                        };
                        result.Add(table);
                    }
                    return new HttpResponse() { Response = result };
                }
                else
                {
                    return new HttpResponse() { Response = profileService.ActiveProfile };
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }
        }

        public static async Task<HttpResponse> GetImage(int quality, int index, Size size)
        {
            HttpResponse response = new HttpResponse();
            try
            {
                IImageHistoryVM hist = AdvancedAPI.Controls.ImageHistory;
                if (hist.ImageHistory.Count <= 0)
                {
                    response.Response = "[]";
                    return response;
                }
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                ImageHistoryPoint p = hist.ImageHistory[index]; // Get the historyPoint at the specified index for the image

                IImageData imageData = await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.LocalPath, 16, true, RawConverterEnum.FREEIMAGE);
                IRenderedImage renderedImage = imageData.RenderImage();

                // Stretch the image for preview, could be made adjustable with url parameters
                renderedImage = await renderedImage.Stretch(profile.ImageSettings.AutoStretchFactor, profile.ImageSettings.BlackClipping, profile.ImageSettings.UnlinkedStretch);
                var bitmap = renderedImage.Image;

                response.Response = ResizeAndConvertBitmap(bitmap, size, quality);
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }
        }

        public static HttpResponse GetSequence()
        {
            ISequenceMediator Sequence = AdvancedAPI.Controls.Sequence;
            HttpResponse response = new HttpResponse();
            try
            {
                if (!Sequence.Initialized)
                {
                    return Utility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                IList<IDeepSkyObjectContainer> targets = Sequence.GetAllTargets();
                if (targets.Count == 0)
                {
                    return Utility.CreateErrorTable(new Error("No DSO Container found", 409));
                }
                response.Response = getSequenceRecursivley(targets[0].Parent.Parent);
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }
        }

        public static HttpResponse GetSocketEventHistory()
        {
            List<object> result = new List<object>();
            foreach (HttpResponse response in WebSocketV2.Events)
            {
                result.Add(response.Response);
            }

            return new HttpResponse() { Response = result };
        }

        public static HttpResponse Screenshot(int quality, Size size)
        {
            HttpResponse response = new HttpResponse();
            Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                    Screen.PrimaryScreen.Bounds.Y,
                                    0, 0,
                                    screenshot.Size,
                                    CopyPixelOperation.SourceCopy);
            }

            BitmapSource source = ImageUtility.ConvertBitmap(screenshot);

            response.Response = ResizeAndConvertBitmap(source, size, quality);

            return response;
        }

        public static string ResizeAndConvertBitmap(BitmapSource source, Size size, int quality)
        {
            string base64;

            if (size != Size.Empty) // Resize the image if requested
            {
                double scaling = size.Width / source.Width;

                source = new TransformedBitmap(source, new ScaleTransform(scaling, scaling));
            }

            if (quality < 0)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));

                base64 = Utility.EncoderToBase64(encoder);
            }
            else
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = quality;
                encoder.Frames.Add(BitmapFrame.Create(source));

                base64 = Utility.EncoderToBase64(encoder);
            }

            return base64;
        }

        private static List<Hashtable> getSequenceRecursivley(ISequenceContainer sequence)
        {
            List<Hashtable> result = new List<Hashtable>();
            foreach (var item in sequence.Items)
            {
                if (item is ISequenceContainer container)
                {
                    result.Add(new Hashtable() 
                    { 
                        { "Name", item.Name + "_Container" } ,
                        { "Status", item.Status.ToString() },
                        { "Description", item.Description },
                        { "Items", getSequenceRecursivley(container) }
                    });
                }
                else
                {
                    result.Add(new Hashtable()
                    {
                        { "Name", item.Name },
                        { "Status", item.Status.ToString() },
                        { "Description", item.Description }
                    });
                }
            }

            return result;
        }

        public static HttpResponse GetAvailableSequences()
        {
            HttpResponse response = new HttpResponse();
            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
            string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;

            List<string> sequences = new List<string>();
            if (Directory.Exists(sequenceFolder))
            {
                foreach (string filename in  Directory.GetFiles(sequenceFolder))
                {
                    sequences.Add(Path.GetFileNameWithoutExtension(filename));
                }
            }

            response.Response = sequences;

            return response;
        }
    }
}
