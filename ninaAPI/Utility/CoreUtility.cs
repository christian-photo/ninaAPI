#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ninaAPI.WebService;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NINA.ViewModel.Sequencer;
using Newtonsoft.Json.Converters;
using System.Globalization;
using ninaAPI.Utility.Http;
using System.Net;
using Newtonsoft.Json.Serialization;
using NINA.Profile.Interfaces;
using System.Windows.Input;
using System.Drawing;
using System.ComponentModel;

namespace ninaAPI.Utility
{
    public static class DelayedAction
    {
        public static void Execute(TimeSpan delay, Action action)
        {
            Task.Delay(delay).ContinueWith(t => action());
        }
    }

    public static class CoreUtility
    {
        static CoreUtility()
        {
            options.Converters.Add(new JsonStringEnumConverter());
        }

        public static ISequenceRootContainer GetSequenceRoot(this ISequenceMediator sequence)
        {
            var navigation = (ISequenceNavigationVM)sequence.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sequence);
            return navigation.Sequence2VM.Sequencer.MainContainer;
        }

        public static IList<IDeepSkyObjectContainer> GetAllTargets(this ISequenceMediator sequence)
        {
            IList<IDeepSkyObjectContainer> targets = sequence.GetAllTargetsInAdvancedSequence();
            targets.Concat(sequence.GetAllTargetsInSimpleSequence());
            return targets;
        }

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

        private static ApplicationStatus Status;
        public static Progress<ApplicationStatus> GetStatus(this IApplicationStatusMediator mediator)
        {
            return new Progress<ApplicationStatus>(p => Status = p);
        }

        public static HttpResponse CreateErrorTable(string message, int code = 500)
        {
            return CreateErrorTable(new Error(message, code));
        }

        public static HttpResponse CreateErrorTable(Error error)
        {
            return new HttpResponse() { Error = error.message, Success = false, StatusCode = error.code };
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

        public static void WriteSequenceResponse(this IHttpContext context, object json)
        {
            context.Response.ContentType = MimeType.Json;

            string text = JsonConvert.SerializeObject(json, sequenceSerializerSettings);

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            context.Response.ContentLength64 = bytes.Length;

            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(text);
            }
        }

        public static void WriteToResponse(this IHttpContext context, object json)
        {
            context.Response.ContentType = MimeType.Json;

            string text = System.Text.Json.JsonSerializer.Serialize(json, options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            context.Response.ContentLength64 = bytes.Length;
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(text);
            }
        }

        public static bool IsParameterOmitted(this IHttpContext context, string parameter)
        {
            return !context.Request.QueryString.AllKeys.Contains(parameter);
        }

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

        public static bool IsBetween(this short value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static bool IsBetween(this int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static bool IsBetween(this decimal value, decimal min, decimal max)
        {
            return value >= min && value <= max;
        }

        public static bool IsBetween(this float value, float min, float max)
        {
            return IsBetween((decimal)value, (decimal)min, (decimal)max);
        }

        public static bool IsBetween(this double value, double min, double max)
        {
            return IsBetween((decimal)value, (decimal)min, (decimal)max);
        }

        public static readonly List<string> IMAGE_TYPES = ["LIGHT", "FLAT", "BIAS", "DARK", "SNAPSHOT"];
    }

    public class HttpResponse
    {
        public const string TypeAPI = "API";
        public const string TypeSocket = "Socket";

        public object Response { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 200;
        public bool Success { get; set; } = true;
        public string Type { get; set; } = TypeAPI;
    }

    public class StringResponse(string message)
    {
        public string Message { get; set; } = message;
    }

    public class StatusResponse(string status)
    {
        public string Status { get; set; } = status;
    }

    public class ResponseFactory
    {
        public static object CreateProcessResponse(ApiProcessStartResult result, Guid id)
        {
            return new { Status = result.ToString(), ProcessId = id };
        }

        public static object CreateProcessConflictsResponse(ApiProcessMediator mediator, ApiProcess process)
        {
            var conflicts = mediator.CheckForConflicts(process.ProcessType, process.ProcessId);
            return new
            {
                Error = HttpUtility.StatusCodeMessages[(int)HttpStatusCode.Conflict],
                Message = $"Process {process.ProcessId} ({process.ProcessType}) could not be started because other processes conflict with it",
                Conflicts = conflicts
            };
        }

        public static (object, int) CreateProcessStartedResponse(ApiProcessStartResult result, ApiProcessMediator mediator, ApiProcess process)
        {
            object response;
            int statusCode = 200;

            if (result == ApiProcessStartResult.Conflict)
            {
                response = CreateProcessConflictsResponse(mediator, process);
                statusCode = (int)HttpStatusCode.Conflict;
            }
            else
            {
                response = CreateProcessResponse(result, process.ProcessId);
            }

            return (response, statusCode);
        }
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
        JXL,
        AVIF,
        JPEG,
        PNG,
        WEBP
    }
}
