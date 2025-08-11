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
using System.Net;
using System.Net.Sockets;
using ninaAPI.WebService;
using System.Net.NetworkInformation;
using NINA.Core.Utility;
using System.Threading.Tasks;
using NINA.Profile.Interfaces;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Drawing;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NINA.ViewModel.Sequencer;

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
            sequenceOptions.ContractResolver = new SequenceIgnoreResolver();
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

        private static readonly Lazy<Dictionary<string, string>> lazyNames = new Lazy<Dictionary<string, string>>(() =>
        {
            var names = new Dictionary<string, string>()
            {
                { "LOCALHOST", "localhost" }
            };

            string hostName = Dns.GetHostName();
            if (!string.IsNullOrEmpty(hostName))
            {
                names.Add("HOSTNAME", hostName);
            }

            string ipv4 = GetIPv4Address();
            if (!string.IsNullOrEmpty(ipv4))
            {
                names.Add("IPADRESS", ipv4);
            }

            return names;
        });

        public static Dictionary<string, string> GetLocalNames()
        {
            return lazyNames.Value;
        }

        public static bool IsPortAvailable(int port)
        {
            bool isPortAvailable = true;

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    isPortAvailable = false;
                    break;
                }
            }

            return isPortAvailable;
        }

        public static int GetNearestAvailablePort(int startPort)
        {
            using var watch = MyStopWatch.Measure();
            int port = startPort;
            while (!IsPortAvailable(port))
            {
                port++;
            }
            return port;
        }

        private static string GetIPv4Address()
        {
            string localIP;
            try
            {
                using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            catch (Exception)
            {
                localIP = "127.0.0.1";
            }

            return localIP;
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

        private static readonly JsonSerializerSettings sequenceOptions = new JsonSerializerSettings()
        {
            Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static void WriteSequenceResponse(this IHttpContext context, object json)
        {
            context.Response.ContentType = MimeType.Json;

            string text = JsonConvert.SerializeObject(json, sequenceOptions);

            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(text);
            }
        }

        public static void WriteToResponse(this IHttpContext context, object json)
        {
            context.Response.ContentType = MimeType.Json;

            /*
            HttpResponse response = (HttpResponse)json;
            context.Response.StatusCode = response.StatusCode;
            response.StatusCode = null;
            if (!string.IsNullOrEmpty(response.Error)) {
                response.Response = response.Error;
                response.Error = null;
            }
            */

            string text = System.Text.Json.JsonSerializer.Serialize(json, options);
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(text);
            }
        }

        public static bool IsParameterOmitted(this IHttpContext context, string parameter)
        {
            return !context.Request.QueryString.AllKeys.Contains(parameter);
        }

        public static object CastString(this string str, Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
            {
                return int.Parse(str);
            }
            if (type == typeof(double) || type == typeof(double?))
            {
                return double.Parse(str);
            }
            if (type == typeof(bool) || type == typeof(bool?))
            {
                return bool.Parse(str);
            }
            if (type == typeof(long) || type == typeof(long?))
            {
                return long.Parse(str);
            }
            if (type == typeof(short) || type == typeof(short?))
            {
                return short.Parse(str);
            }
            if (type.IsEnum)
            {
                if (int.TryParse(str, out int x))
                {
                    return Enum.ToObject(type, x);
                }
                return Enum.Parse(type, str);
            }
            return str;
        }

        public static string[] GetFilesRecursively(string path)
        {
            List<string> files = [.. Directory.GetFiles(path)];
            foreach (string dir in Directory.GetDirectories(path))
            {
                files.AddRange(GetFilesRecursively(dir));
            }
            return [.. files];
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

    public class SequenceIgnoreResolver : DefaultContractResolver
    {
        private static readonly string[] ignoredProperties = ["UniversalPolarAlignmentVM", "Latitude", "Longitude", "Elevation", "AltitudeSite", "ShiftTrackingRate",
            "DateTime", "Expanded", "DateTimeProviders", "Horizon", "Parent", "InfoButtonColor", "Icon"];

        private static readonly Type[] ignoredTypes = [typeof(IProfile), typeof(IProfileService), typeof(CustomHorizon), typeof(ICommand), typeof(AsyncRelayCommand), typeof(CommunityToolkit.Mvvm.Input.RelayCommand), typeof(Icon), typeof(Func<>), typeof(Action<>)];

        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            Newtonsoft.Json.Serialization.JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (ignoredProperties.Contains(property.PropertyName) || ignoredTypes.Contains(property.PropertyType))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}
