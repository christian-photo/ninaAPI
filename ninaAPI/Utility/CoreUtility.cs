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
using ninaApi.Utility.Serialization;

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

        public static object CastString(this string str, Type type)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            str = str.Trim();

            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int))
                return int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
            if (underlyingType == typeof(double))
                return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
            if (underlyingType == typeof(bool))
                return bool.TryParse(str, out var b) ? b : false;
            if (underlyingType == typeof(long))
                return long.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : 0L;
            if (underlyingType == typeof(short))
                return short.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : (short)0;
            if (underlyingType == typeof(float))
                return float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : 0f;
            if (underlyingType == typeof(decimal))
                return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var m) ? m : 0m;
            if (underlyingType == typeof(byte))
                return byte.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var by) ? by : (byte)0;
            if (underlyingType == typeof(DateTime))
                return DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue;
            if (underlyingType == typeof(string))
                return str;
            if (underlyingType.IsEnum)
            {
                if (int.TryParse(str, out int x))
                    return Enum.ToObject(underlyingType, x);
                return Enum.TryParse(underlyingType, str, true, out var result) ? result : Activator.CreateInstance(underlyingType);
            }

            return str;
        }
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

    public class StringResponse
    {
        public string Message { get; set; }
    }

    public class StatusResponse
    {
        public string Status { get; set; }
    }

    public enum Device
    {
        Camera,
        Dome,
        Filterwheel,
        Focuser,
        Guider,
        Mount,
        Rotator,
        Safetymonitor,
        Switch,
        Weather,
    }
}
