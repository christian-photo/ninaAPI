#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using ninaAPI.WebService;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NINA.ViewModel.Sequencer;
using Newtonsoft.Json.Converters;
using System.Globalization;
using ninaAPI.Utility.Http;
using Newtonsoft.Json.Serialization;
using NINA.Profile.Interfaces;
using System.Windows.Input;
using System.Drawing;
using System.ComponentModel;

namespace ninaAPI.Utility
{
    public static class ExtensionMethods
    {
        public static ISequenceRootContainer GetSequenceRoot(this ISequenceMediator sequence)
        {
            var navigation = (ISequenceNavigationVM)sequence.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sequence);
            return navigation.Sequence2VM.Sequencer.MainContainer;
        }

        public static IList<IDeepSkyObjectContainer> GetAllTargets(this ISequenceMediator sequence)
        {
            IList<IDeepSkyObjectContainer> targets = sequence.GetAllTargetsInAdvancedSequence();
            return targets;
        }

        private static ApplicationStatus Status;
        public static Progress<ApplicationStatus> GetStatus(this IApplicationStatusMediator mediator)
        {
            return new Progress<ApplicationStatus>(p => Status = p);
        }

        /// <summary>
        /// Checks if a value is between two values, both inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>True if value >= min and value <= max</returns>
        public static bool IsBetween(this short value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Checks if a value is between two values, both inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>True if value >= min and value <= max</returns>
        public static bool IsBetween(this int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Checks if a value is between two values, both inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>True if value >= min and value <= max</returns>
        public static bool IsBetween(this decimal value, decimal min, decimal max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Checks if a value is between two values, both inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>True if value >= min and value <= max</returns>
        public static bool IsBetween(this float value, float min, float max)
        {
            return IsBetween((decimal)value, (decimal)min, (decimal)max);
        }

        /// <summary>
        /// Checks if a value is between two values, both inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>True if value >= min and value <= max</returns>
        public static bool IsBetween(this double value, double min, double max)
        {
            return IsBetween((decimal)value, (decimal)min, (decimal)max);
        }
    }

    public static class ResponseV2ExtensionMethods
    {
        static ResponseV2ExtensionMethods()
        {
            options.Converters.Add(new JsonStringEnumConverter());
        }

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        private static readonly JsonSerializerSettings sequenceSerializerSettings = new JsonSerializerSettings()
        {
            Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
            ContractResolver = new SequenceIgnoreResolver(),
            FloatFormatHandling = FloatFormatHandling.String,
        };

        public static void WriteSequenceResponse(this SimpleW.HttpResponse context, object json)
        {
            string text = JsonConvert.SerializeObject(json, sequenceSerializerSettings);

            context.Text(text, "application/json").SendAsync().AsTask().Wait();
        }

        public static void WriteToResponse(this SimpleW.HttpResponse context, object json)
        {
            string text = System.Text.Json.JsonSerializer.Serialize(json, options);

            context.Text(text, "application/json").SendAsync().AsTask().Wait();
        }
    }

    public static class ReflectionHelper
    {
        /// <summary>
        /// Copies properties from source to target. Ensures that:
        /// - Properties in the source type exist in the target type
        /// - Properties in the source type have the same type as the ones in the target type
        /// - Properties can actually be written
        /// - Properties are public and instance members (not static)
        /// </summary>
        public static void CopyProperties(object source, object target)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();

            foreach (var property in sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var targetProperty = targetType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (targetProperty != null && targetProperty.CanWrite && targetProperty.PropertyType == property.PropertyType)
                {
                    targetProperty.SetValue(target, property.GetValue(source));
                }
            }
        }

        /// <summary>
        /// Converts a string to the specified type. This method handles Nullable<T> types.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="type">The target type.</param>
        /// <returns>The converted value.</returns>
        public static object ConvertString(this string str, Type type)
        {
            // determine target (handle Nullable<T>)
            var targetType = Nullable.GetUnderlyingType(type) ?? type;

            object converted;

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
                converted = converter.ConvertFromInvariantString(str);
            else
                converted = Convert.ChangeType(str, targetType, CultureInfo.InvariantCulture);

            return converted;
        }

        /// <summary>
        /// Traverses the object `position` as specified by the `pathDescription` and sets the value to `value`.
        /// </summary>
        public static void SetValueReflected(object position, string pathDescription, object value)
        {
            string[] pathSplit = pathDescription.Split('-'); // e.g. 'CameraSettings-PixelSize' -> CameraSettings, PixelSize

            if (pathSplit.Length == 1)
            {
                var prop = position.GetType().GetProperty(pathDescription);
                // This is needed because (as an example) Newtonsoft.JSON by default deserializes to double, and an assignment to a float would fail
                var converted = Convert.ChangeType(value, prop.PropertyType);
                prop.SetValue(position, converted);
            }
            else
            {
                for (int i = 0; i <= pathSplit.Length - 2; i++)
                {
                    if (IsIndexable(position, out Type indexType, out PropertyInfo indexProp))
                    {
                        position = indexProp.GetValue(position, [indexType == typeof(string) ? pathSplit[i] : int.Parse(pathSplit[i])]);
                    }
                    else
                    {
                        position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
                    }
                }
                PropertyInfo prop = position.GetType().GetProperty(pathSplit[^1]);
                // This is needed because (as an example) Newtonsoft.JSON by default deserializes to double, and an assignment to a float would fail
                var converted = Convert.ChangeType(value, prop.PropertyType);
                prop.SetValue(position, converted);
            }
        }

        /// <summary>
        /// Returns true if the object is indexable (i.e., has an indexer), and returns the type of the indexer and the property that represents the indexer. Returns false otherwise.
        /// </summary>
        private static bool IsIndexable(object obj, out Type indexType, out PropertyInfo indexProp)
        {
            indexProp = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.GetIndexParameters().Length > 0, null);
            if (indexProp == null)
            {
                indexType = null;
                return false;
            }

            indexType = indexProp.GetIndexParameters()[0].ParameterType;
            return true;
        }
    }


    public static class CoreUtility
    {
        public static CustomResponse CreateErrorTable(string message, int code = 500)
        {
            return CreateErrorTable(new Error(message, code));
        }

        public static CustomResponse CreateErrorTable(Error error)
        {
            return new CustomResponse() { Error = error.message, Success = false, StatusCode = error.code };
        }

        public static readonly List<string> IMAGE_TYPES = ["LIGHT", "FLAT", "BIAS", "DARK", "SNAPSHOT"];
    }

    internal class SequenceIgnoreResolver : DefaultContractResolver
    {
        private static readonly string[] ignoredProperties = ["UniversalPolarAlignmentVM", "Latitude", "Longitude", "Elevation", "AltitudeSite", "ShiftTrackingRate",
            "DateTime", "Expanded", "DateTimeProviders", "Horizon", "Parent", "InfoButtonColor", "Icon"];

        private static readonly Type[] ignoredTypes = [typeof(IProfile), typeof(IProfileService), typeof(CustomHorizon), typeof(ICommand), typeof(CommunityToolkit.Mvvm.Input.AsyncRelayCommand), typeof(CommunityToolkit.Mvvm.Input.RelayCommand), typeof(Icon), typeof(Func<>), typeof(Action<>)];

        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            Newtonsoft.Json.Serialization.JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (ignoredProperties.Contains(property.PropertyName) || ignoredTypes.Any(t => t.IsAssignableFrom(property.PropertyType)))
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }

    public enum Device
    {
        Camera,
        Dome,
        Filterwheel,
        FlatDevice,
        Focuser,
        Guider,
        Mount,
        Rotator,
        Safetymonitor,
        Switch,
        Weather,
    }

    public enum ImageFormat
    {
        // JXL,
        // AVIF,
        JPEG,
        PNG,
        // WEBP
    }
}
