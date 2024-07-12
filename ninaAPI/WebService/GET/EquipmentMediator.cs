#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Visualizations;
using ASCOM.Com;
using Newtonsoft.Json;
using NINA.Astrometry;
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
using OxyPlot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace ninaAPI.WebService.GET
{
    public class EquipmentMediator
    {
        public static HttpResponse GetDeviceInfo(EquipmentType deviceType, string parameter)
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

                case EquipmentType.SafteyMonitor:
                    ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;
                    response.Response = safety.GetInfo().GetAllProperties();
                    return response;

                case EquipmentType.Telescope:
                    ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;
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

        public static HttpResponse GetSocketImageHistory(int id)
        {
            if (id < 0)
            {
                return WebSocket.Images[WebSocket.Images.Count - 1];
            }
            if (id >= WebSocket.Images.Count)
            {
                Utility.CreateErrorTable("Index out of range");
            }
            return WebSocket.Images[id];
        }

        public static HttpResponse GetSocketImageCount()
        {
            HttpResponse response = new HttpResponse();
            response.Response = WebSocket.Images.Count;
            return response;
        }

        public static HttpResponse GetImageHistory(int id)
        {
            List<Hashtable> result = new List<Hashtable>();
            ImageHistoryPoint point;

            try
            {
                IImageHistoryVM imaging = AdvancedAPI.Controls.ImageHistory;

                if (id == -1)
                {
                    foreach (ImageHistoryPoint Imagepoint in imaging.ImageHistory)
                    {
                        Hashtable tempTable = new Hashtable()
                        {
                            { "AutoFocusPoint", JsonConvert.SerializeObject(Imagepoint.AutoFocusPoint) },
                            { "DateTime", Imagepoint.dateTime },
                            { "Duration", Imagepoint.Duration },
                            { "Filename", Imagepoint.Filename },
                            { "Filter", Imagepoint.Filter },
                            { "FocuserPosition", Imagepoint.FocuserPosition },
                            { "HFR", Imagepoint.HFR },
                            { "Id", Imagepoint.Id },
                            { "Index", Imagepoint.Index },
                            { "IsBayered", Imagepoint.IsBayered },
                            { "LocalPath", Imagepoint.LocalPath },
                            { "MAD", Imagepoint.MAD },
                            { "Mean", Imagepoint.Mean },
                            { "Median", Imagepoint.Median },
                            { "Rms", Imagepoint.Rms },
                            { "RmsText", Imagepoint.RmsText },
                            { "RotatorMechanicalPosition", Imagepoint.RotatorMechanicalPosition },
                            { "RotatorPosition", Imagepoint.RotatorPosition },
                            { "Stars", Imagepoint.Stars },
                            { "StDev", Imagepoint.StDev },
                            { "TargetName", Imagepoint.Target.Name },
                            { "Temperature", Imagepoint.Temperature },
                            { "Type", Imagepoint.Type }
                        };
                        result.Add(tempTable);
                    }
                    return new HttpResponse() { Response = result };
                }

                point = imaging.ImageHistory[id];
                Hashtable table = new Hashtable()
                {
                    { "AutoFocusPoint", JsonConvert.SerializeObject(point.AutoFocusPoint) },
                    { "DateTime", point.dateTime },
                    { "Duration", point.Duration },
                    { "Filename", point.Filename },
                    { "Filter", point.Filter },
                    { "FocuserPosition", point.FocuserPosition },
                    { "HFR", point.HFR },
                    { "Id", point.Id },
                    { "Index", point.Index },
                    { "IsBayered", point.IsBayered },
                    { "LocalPath", point.LocalPath },
                    { "MAD", point.MAD },
                    { "Mean", point.Mean },
                    { "Median", point.Median },
                    { "Rms", point.Rms },
                    { "RmsText", point.RmsText },
                    { "RotatorMechanicalPosition", point.RotatorMechanicalPosition },
                    { "RotatorPosition", point.RotatorPosition },
                    { "Stars", point.Stars },
                    { "StDev", point.StDev },
                    { "TargetName", point.Target.Name },
                    { "Temperature", point.Temperature },
                    { "Type", point.Type }
                };
                result.Add(table);
                return new HttpResponse() { Response = result };

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(ex.Message);
            }
        }

        public static HttpResponse GetProfile(string id)
        {
            try
            {
                List<Hashtable> result = new List<Hashtable>();
                IProfileService profileService = AdvancedAPI.Controls.Profile;

                if (id.Equals("all"))
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
                else if (id.Equals("active"))
                {
                    return new HttpResponse() { Response = profileService.ActiveProfile };
                }
                return Utility.CreateErrorTable("Unknown parameter");
                
            } 
            catch(Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(ex.Message);
            }
        }

        public static async Task<HttpResponse> GetImage(int jpgQuality, int index)
        {
            HttpResponse response = new HttpResponse();
            try
            {
                IImageHistoryVM hist = AdvancedAPI.Controls.ImageHistory;
                if (hist.ImageHistory.Count <= 0)
                {
                    response.Response = "No Images available";
                    return response;
                }
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                ImageHistoryPoint p;
                if (index < 0)
                    p = hist.ImageHistory[^1];
                else
                    p = hist.ImageHistory[index];
                IImageData imageData = await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.LocalPath, 16, true, RawConverterEnum.FREEIMAGE);
                IRenderedImage renderedImage = imageData.RenderImage();

                renderedImage = await renderedImage.Stretch(profile.ImageSettings.AutoStretchFactor, profile.ImageSettings.BlackClipping, profile.ImageSettings.UnlinkedStretch);

                if (jpgQuality < 0)
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderedImage.Image));

                    response.Response = Utility.EncoderToBase64(encoder);
                }
                else
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = jpgQuality;
                    encoder.Frames.Add(BitmapFrame.Create(renderedImage.Image));

                    response.Response = Utility.EncoderToBase64(encoder);
                }
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(ex.Message);
            }
        }

        public static async Task<HttpResponse> GetThumbnail(int quality, int index)
        {
            HttpResponse response = new HttpResponse();
            try
            {
                IImageHistoryVM hist = AdvancedAPI.Controls.ImageHistory;
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                if (hist.ImageHistory.Count <= 0)
                {
                    response.Response = "No images available";
                }
                ImageHistoryPoint p;
                if (index < 0)
                    p = hist.ImageHistory[^1];
                else
                    p = hist.ImageHistory[index];
                IImageData imageData = await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.LocalPath, 16, false, RawConverterEnum.FREEIMAGE);
                IRenderedImage renderedImage = imageData.RenderImage();

                renderedImage = await renderedImage.Stretch(profile.ImageSettings.AutoStretchFactor, profile.ImageSettings.BlackClipping, profile.ImageSettings.UnlinkedStretch);
                double scaling = 640.0d / renderedImage.Image.Width;

                var bitmap = new TransformedBitmap(renderedImage.Image, new ScaleTransform(scaling, scaling));

                if (quality < 0)
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    response.Response = Utility.EncoderToBase64(encoder);
                }
                else
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = quality;
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    response.Response = Utility.EncoderToBase64(encoder);
                }
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(ex.Message);
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
                    return Utility.CreateErrorTable("Sequence is not initialized");
                }
                IList<IDeepSkyObjectContainer> targets = Sequence.GetAllTargets();
                if (targets.Count == 0)
                {
                    return Utility.CreateErrorTable("Sequence is empty");
                }
                response.Response = JsonConvert.DeserializeObject(
                    JsonConvert.SerializeObject(
                        (SequenceRootContainer)targets[0].Parent.Parent,
                        typeof(SequenceRootContainer),
                        new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = new IgnorePropertiesResolver(new[] { "Parent" }) }));
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Utility.CreateErrorTable(ex.Message);
            }
        }

        public static HttpResponse GetImageCount()
        {
            IImageHistoryVM imaging = AdvancedAPI.Controls.ImageHistory;
            Hashtable temp = new Hashtable();
            temp.Add("Count", imaging.ImageHistory.Count());
            return new HttpResponse() { Response = temp };
        }

        private static Hashtable GetAllProperties<T>(T instance)
        {
            Hashtable result = new Hashtable();
            List<object> visited = new List<object>();
            try
            {
                foreach (PropertyInfo info in instance.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == 0))
                {
                    var obj = info.GetValue(instance);
                    if (obj is null)
                    {
                        continue;
                    }
                    else if (visited.Contains(obj))
                    {
                        continue;
                    }
                    else if (obj.GetType() == typeof(List<DataPoint>))
                    {
                        continue;
                    }
                    visited.Add(obj);

                    if (result.ContainsKey(info.Name)) continue;

                    if (obj is DateTime)
                    {
                        result.Add(info.Name, obj.ToString());
                        continue;
                    }
                    else if (obj is string)
                    {
                        result.Add(info.Name, obj.ToString());
                        continue;
                    }
                    else if (obj.GetType().IsEnum)
                    {
                        result.Add(info.Name, obj.ToString());
                        continue;
                    }
                    else if (obj.GetType().IsArray)
                    {
                        List<Hashtable> tables = new List<Hashtable>();

                        foreach (object item in obj as Object[])
                        {
                            tables.Add(GetAllProperties(item));
                        }
                        result.Add(info.Name, tables);
                        continue;
                    }
                    else if (obj is IList)
                    {
                        List<Hashtable> tables = new List<Hashtable>();
                        foreach (object item in obj as IList)
                        {
                            tables.Add(GetAllProperties(item));
                        }
                        result.Add(info.Name, tables);
                        continue;
                    }
                    else if (obj is ICustomDateTime)
                    {
                        continue;
                    }
                    else if (obj is Histogram)
                    {
                        continue;
                    }
                    else if (obj is CultureInfo)
                    {
                        continue;
                    }
                    else if (obj is IColorSchemaSettings)
                    {
                        continue;
                    }
                    else if (obj is NighttimeData)
                    {
                        continue;
                    }
                    else if (obj is ICommand)
                    {
                        continue;
                    }
                    else if (obj is RelayCommand)
                    {
                        continue;
                    }
                    else if (obj is Coordinates)
                    {
                        continue;
                    }
                    else if (obj is GeometryGroup)
                    {
                        continue;
                    }
                    else if (info.Name == "ConditionWatchdog") continue;
                    else if (obj is ISequenceContainer) continue;

                    if (!info.PropertyType.IsPrimitive)
                    {
                        Hashtable props = GetAllProperties(obj);
                        result.Add(info.Name, props);
                        continue;
                    }
                    result.Add(info.Name, obj.ToString());
                }
            } catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;
        }
    }
}
