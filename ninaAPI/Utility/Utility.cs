#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ninaAPI
{
    public static class Utility
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

        public static string MakeString(this IEnumerable<string> list)
        {
            return "[" + string.Join(", ", list) + "]";
        }

        public static string MakeString(this Dictionary<string, string>.ValueCollection coll)
        {
            return coll.ToArray().MakeString();
        }

        public static Dictionary<string, string> GetLocalNames()
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            names.Add("LOCALHOST", "localhost");

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

            Logger.Debug("Local names: " + names.Values.MakeString());

            return names;
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

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
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

        public static HttpResponse CreateErrorTable(string message)
        {
            return new HttpResponse() { Error = message, Success = false };
        }

        public static void WriteToResponse(this IHttpContext context, string json)
        {
            context.Response.ContentType = MimeType.Json;
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(json);
            }
        }
        
        public static void WriteToResponse(this IHttpContext context, object json, JsonSerializerSettings settings = null)
        {
            context.Response.ContentType = MimeType.Json;
            if (settings == null)
            {
                settings = new JsonSerializerSettings();
            }
            string text = JsonConvert.SerializeObject(json, settings);
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(text);
            }
        }

        public static Hashtable GetAllProperties(this object obj)
        {
            Hashtable table = new Hashtable();
            foreach (var prop in obj.GetType().GetProperties())
            {
                table.Add(prop.Name, prop.GetValue(obj));
            }
            return table;
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
    }

    public class HttpResponse
    {
        public static string TypeAPI = "API";
        public static string TypeSocket = "Socket";
        
        public object Response { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string Type { get; set; } = TypeAPI;
    }

    public class DynamicContractResolver : DefaultContractResolver
    {

        private Type _typeToIgnore;
        public DynamicContractResolver(Type typeToIgnore)
        {
            _typeToIgnore = typeToIgnore;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            properties = properties.Where(p => p.PropertyType != _typeToIgnore).ToList();

            return properties;
        }
    }

    public enum EquipmentType
    {
        Camera,
        Telescope,
        FilterWheel,
        Focuser,
        Dome,
        Rotator,
        Guider,
        FlatDevice,
        Switch,
        SafteyMonitor
    }
}
