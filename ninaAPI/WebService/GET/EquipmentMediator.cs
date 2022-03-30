#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Visualizations;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace ninaAPI.WebService.GET
{
    public class EquipmentMediator
    {
        public static Hashtable GetCamera(string property)
        {
            try
            {
                ICameraMediator camera = AdvancedAPI.Controls.Camera;

                if (property.Equals("all"))
                {
                    return GetAllProperties(camera.GetInfo());
                }
                object val = camera.GetInfo().GetType().GetProperty(property).GetValue(camera.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { camera.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetTelescope(string property)
        {
            try
            {
                ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;

                if (property.Equals("all"))
                {
                    return GetAllProperties(telescope.GetInfo());
                }
                object val = telescope.GetInfo().GetType().GetProperty(property).GetValue(telescope.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { telescope.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetFocuser(string property)
        {
            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;

                if (property.Equals("all"))
                {
                    return GetAllProperties(focuser.GetInfo());
                }
                object val = focuser.GetInfo().GetType().GetProperty(property).GetValue(focuser.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { focuser.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetFilterWheel(string property)
        {
            try
            {
                IFilterWheelMediator filterWheel = AdvancedAPI.Controls.FilterWheel;

                if (property.Equals("all"))
                {
                    return GetAllProperties(filterWheel.GetInfo());
                }
                object val = filterWheel.GetInfo().GetType().GetProperty(property).GetValue(filterWheel.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { filterWheel.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetGuider(string property)
        {
            try
            {
                IGuiderMediator guider = AdvancedAPI.Controls.Guider;

                if (property.Equals("all"))
                {
                    return GetAllProperties(guider.GetInfo());
                }
                object val = guider.GetInfo().GetType().GetProperty(property).GetValue(guider.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { guider.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetDome(string property)
        {
            try
            {
                IDomeMediator dome = AdvancedAPI.Controls.Dome;

                if (property.Equals("all"))
                {
                    return GetAllProperties(dome.GetInfo());
                }
                object val = dome.GetInfo().GetType().GetProperty(property).GetValue(dome.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { dome.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetRotator(string property)
        {
            try
            {
                IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

                if (property.Equals("all"))
                {
                    return GetAllProperties(rotator.GetInfo());
                }
                object val = rotator.GetInfo().GetType().GetProperty(property).GetValue(rotator.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { rotator.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetSafetyMonitor(string property)
        {
            try
            {
                ISafetyMonitorMediator safetyMonitor = AdvancedAPI.Controls.SafetyMonitor;

                if (property.Equals("all"))
                {
                    return GetAllProperties(safetyMonitor.GetInfo());
                }
                object val = safetyMonitor.GetInfo().GetType().GetProperty(property).GetValue(safetyMonitor.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { safetyMonitor.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetFlatDevice(string property)
        {
            try
            {
                IFlatDeviceMediator flatDevice = AdvancedAPI.Controls.FlatDevice;

                if (property.Equals("all"))
                {
                    return GetAllProperties(flatDevice.GetInfo());
                }
                object val = flatDevice.GetInfo().GetType().GetProperty(property).GetValue(flatDevice.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double)
                {
                    return new Hashtable
                    {
                        { flatDevice.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static Hashtable GetSwitch(string property)
        {
            try
            {
                ISwitchMediator switches = AdvancedAPI.Controls.Switch;

                if (property.Equals("all"))
                {
                    return GetAllProperties(switches.GetInfo());
                }
                object val = switches.GetInfo().GetType().GetProperty(property).GetValue(switches.GetInfo());
                if (val is string || val is bool || val is int || val is float || val is double) 
                {
                    return new Hashtable
                    {
                        { switches.GetInfo().GetType().GetProperty(property).Name, val.ToString() }
                    };
                }
                return GetAllProperties(val);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return new Hashtable();
        }

        public static List<Hashtable> GetImageHistory(string id)
        {
            List<Hashtable> result = new List<Hashtable>();
            ImageHistoryPoint point;

            try
            {
                IImageHistoryVM imaging = AdvancedAPI.Controls.ImageHistory;

                if (id.Equals("all"))
                {
                    foreach (ImageHistoryPoint Imagepoint in imaging.ImageHistory)
                    {
                        result.Add(GetAllProperties(Imagepoint));
                    }
                    return result;
                }

                point = imaging.ImageHistory[int.Parse(id)];
                result.Add(GetAllProperties(point));
                return result;

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;
        }

        public static List<Hashtable> GetProfile(string id)
        {
            List<Hashtable> result = new List<Hashtable>();

            try
            {
                IProfileService profileService = AdvancedAPI.Controls.Profile;

                if (id.Equals("all"))
                {
                    foreach (ProfileMeta Profile in profileService.Profiles)
                    {
                        result.Add(GetAllProperties(Profile));
                    }
                    return result;
                }
                else if (id.Equals("active"))
                {
                    result.Add(GetAllProperties(profileService.ActiveProfile));
                    return result;
                }
            } catch(Exception ex)
            {
                Logger.Error(ex);
            }
            return new List<Hashtable>();
        }

        public static async Task<List<Hashtable>> GetSequence(string property)
        {
            ISequenceMediator Sequence = AdvancedAPI.Controls.Sequence;
            List<Hashtable> result = new List<Hashtable>();
            try
            {
                if (!Sequence.Initialized)
                {
                    result.Add(new Hashtable() { { "Initialized", "false" } });
                    return result;
                }
                if (property.Equals("all"))
                {
                    IList<IDeepSkyObjectContainer> AllTargets = Sequence.GetAllTargetsInAdvancedSequence();
                    AllTargets.Concat(Sequence.GetAllTargetsInSimpleSequence());
                    foreach (IDeepSkyObjectContainer Target in AllTargets)
                    {
                        
                        result.Add(GetAllProperties(Target.Parent));
                    }
                    return result;
                }
                else if (property.Equals("image"))
                {
                    IImageHistoryVM hist = AdvancedAPI.Controls.ImageHistory;
                    ImageHistoryPoint p = hist.ImageHistory[hist.ImageHistory.Count - 1];
                    IImageData imageData = await AdvancedAPI.Controls.ImageDataFactory.CreateFromFile(p.LocalPath, 16, false, NINA.Core.Enum.RawConverterEnum.FREEIMAGE);
                    IRenderedImage renderedImage = imageData.RenderImage();

                    renderedImage = await renderedImage.Stretch(0.15, -2.8, true);

                    Hashtable res = new Hashtable();

                    res["Image"] = Utility.BitmapToBase64(Utility.BitmapFromSource(renderedImage.Image));
                    res["Success"] = true;
                    result.Add(res);
                    return result;
                }
                IList<IDeepSkyObjectContainer> targets = Sequence.GetAllTargetsInAdvancedSequence();
                targets.Concat(Sequence.GetAllTargetsInSimpleSequence());
                result.Add(GetAllProperties(targets[int.Parse(property)]));
                return result;
            } catch (Exception ex)
            {
                Logger.Error(ex);
            }

            
            return new List<Hashtable>();
        }

        public static Hashtable GetSequenceCount()
        {
            ISequenceMediator Sequence = AdvancedAPI.Controls.Sequence;
            Hashtable result = new Hashtable();
            IList<IDeepSkyObjectContainer> AllTargets = Sequence.GetAllTargetsInAdvancedSequence();
            AllTargets.Concat(Sequence.GetAllTargetsInSimpleSequence());
            result.Add("Count", AllTargets.Count);
            return result;
        }

        public static Hashtable GetImageCount()
        {
            IImageHistoryVM imaging = AdvancedAPI.Controls.ImageHistory;
            Hashtable result = new Hashtable();
            result.Add("Count", imaging.ImageHistory.Count());
            return result;
        }

        private static Hashtable GetAllProperties(object instance)
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
                        Logger.Debug($"{info.Name} is Array");
                        List<Hashtable> tables = new List<Hashtable>();

                        foreach (object item in obj as Object[])
                        {
                            tables.Add(GetAllProperties(item));
                        }
                        result.Add(info.Name, tables);
                        continue;
                    }
                    else if (obj is IList && obj.GetType().IsGenericType)
                    {
                        Logger.Debug($"{info.Name} is List");
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
                    else if (obj is ISequenceContainer) continue;

                    if (!info.PropertyType.IsPrimitive)
                    {
                        Logger.Debug($"{info.Name} is primitive ({info.PropertyType.Name})");
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
