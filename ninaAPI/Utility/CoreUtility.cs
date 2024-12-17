#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media.Imaging;
using ninaAPI.WebService;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ninaAPI.Utility
{
    public static class CoreUtility
    {
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

        private static string GetIPv4Address()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        public static string BitmapToBase64(Bitmap bmp)
        {
            Bitmap map = new Bitmap(bmp);
            using (MemoryStream memory = new MemoryStream())
            {
                map.Save(memory, ImageFormat.Png);
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        public static string BitmapToBase64(Bitmap bmp, int jpgQuality)
        {
            //Bitmap map = new Bitmap(bmp.Width, bmp.Height);
            using (MemoryStream memory = new MemoryStream())
            {
                bmp.Save(memory, GetEncoder(ImageFormat.Jpeg), GetCompression(jpgQuality)); // backup compressed copy of image
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        public static string EncoderToBase64(JpegBitmapEncoder encoder)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        public static string EncoderToBase64(PngBitmapEncoder encoder)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        public static HttpResponse CreateErrorTable(string message, int code = 500)
        {
            return CreateErrorTable(new Error(message, code));
        }

        public static HttpResponse CreateErrorTable(Error error)
        {
            return new HttpResponse() { Error = error.message, Success = false, StatusCode = error.code };
        }

        private static JsonSerializerOptions options = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        public static void WriteToResponse(this IHttpContext context, object json)
        {
            context.Response.ContentType = MimeType.Json;

            string text = System.Text.Json.JsonSerializer.Serialize(json, options);

            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(text);
            }
        }

        public static object CastString(this string str, Type type)
        {
            if (type == typeof(int))
            {
                return int.Parse(str);
            }
            if (type == typeof(double))
            {
                return double.Parse(str);
            }
            if (type == typeof(bool))
            {
                return bool.Parse(str);
            }
            if (type == typeof(long))
            {
                return long.Parse(str);
            }
            if (type == typeof(short))
            {
                return short.Parse(str);
            }
            return str;
        }

        /// <summary>
        /// 1 = Lowest Quality
        /// 25 = Low Quality
        /// 50 = Medium Quality
        /// 75 = High Quality
        /// 100 = Highest Quality
        /// </summary>
        /// <returns></returns>
        public static EncoderParameters GetCompression(int quality)
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            return encoderParameters;
        }

        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }

    public class HttpResponse
    {
        public static string TypeAPI = "API";
        public static string TypeSocket = "Socket";

        public object Response { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 200;
        public bool Success { get; set; } = true;
        public string Type { get; set; } = TypeAPI;
    }

    public enum EquipmentType
    {
        Camera,
        Mount,
        FilterWheel,
        Focuser,
        Dome,
        Rotator,
        Guider,
        FlatDevice,
        Switch,
        SafetyMonitor,
        Weather
    }

    public class IgnorePropertiesResolver : DefaultContractResolver
    {
        private readonly HashSet<string> ignoreProps;
        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            Newtonsoft.Json.Serialization.JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (this.ignoreProps.Contains(property.PropertyName))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}
