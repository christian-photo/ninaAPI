#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NINA.Core.Model;
using NINA.Profile.Interfaces;

namespace ninaApi.Utility.Serialization
{
    public class NewtonsoftSerializer : ISerializerService
    {
        private static readonly JsonSerializerSettings sequenceSerializerSettings = new JsonSerializerSettings()
        {
            Error = delegate (object sender, ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
            ContractResolver = new SequenceIgnoreResolver(),
            FloatFormatHandling = FloatFormatHandling.String,
        };

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            Error = delegate (object sender, ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
            ContractResolver = new FieldIgnoreResolver(),
            FloatFormatHandling = FloatFormatHandling.String,
        };
        public string Serialize(object obj, bool isSequence = false)
        {
            return JsonConvert.SerializeObject(obj, isSequence ? sequenceSerializerSettings : serializerSettings);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

    internal class SequenceIgnoreResolver : DefaultContractResolver
    {
        private static readonly string[] ignoredProperties = ["UniversalPolarAlignmentVM", "Latitude", "Longitude", "Elevation", "AltitudeSite", "ShiftTrackingRate",
            "DateTime", "Expanded", "DateTimeProviders", "Horizon", "Parent", "InfoButtonColor", "Icon"];

        private static readonly Type[] ignoredTypes = [typeof(IProfile), typeof(IProfileService), typeof(CustomHorizon), typeof(ICommand), typeof(AsyncRelayCommand), typeof(RelayCommand), typeof(Icon), typeof(Func<>), typeof(Action<>)];

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (ignoredProperties.Contains(property.PropertyName) || ignoredTypes.Contains(property.PropertyType))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }

    internal class FieldIgnoreResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (member is FieldInfo)
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}