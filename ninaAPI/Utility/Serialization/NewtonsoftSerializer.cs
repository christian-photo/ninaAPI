#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;

namespace ninaAPI.Utility.Serialization
{
    public class NewtonsoftSerializer : ISerializerService
    {
        private static readonly JsonSerializerSettings sequenceSerializerSettings = new JsonSerializerSettings()
        {
            Error = delegate (object sender, ErrorEventArgs args)
            {
                Logger.Error(args.ErrorContext.Error);
                args.ErrorContext.Handled = true;
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
            ContractResolver = new SequenceResolver(),
            FloatFormatHandling = FloatFormatHandling.String,
        };

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            Error = delegate (object sender, ErrorEventArgs args)
            {
                Logger.Error(args.ErrorContext.Error);
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

    internal class SequenceResolver : DefaultContractResolver
    {
        private static readonly string[] includedNames = ["Name", "Status"];
        private static readonly string[] excludedNames = ["Parent"];
        private static readonly Type[] excludedTypes = [typeof(Expression)];

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (includedNames.Contains(property.PropertyName))
            {
                property.Ignored = false;
                property.ShouldSerialize = _ => true;
            }
            else if (excludedNames.Contains(property.PropertyName) || excludedTypes.Any(t => t.IsAssignableFrom(property.PropertyType)))
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            if (typeof(ISequenceContainer).IsAssignableFrom(type))
            {
                properties.Add(new JsonProperty
                {
                    PropertyName = "IsContainer",
                    PropertyType = typeof(bool),
                    ValueProvider = new ConstantValueProvider(true),
                    Readable = true,
                    Ignored = false,
                    Writable = false
                });
            }
            else if (typeof(ISequenceEntity).IsAssignableFrom(type))
            {
                properties.Add(new JsonProperty
                {
                    PropertyName = "IsContainer",
                    PropertyType = typeof(bool),
                    ValueProvider = new ConstantValueProvider(false),
                    Readable = true,
                    Ignored = false,
                    Writable = false
                });
            }

            return properties;
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

    public class ConstantValueProvider : IValueProvider
    {
        private readonly object _value;

        public ConstantValueProvider(object value)
        {
            _value = value;
        }

        public object GetValue(object target)
        {
            return _value;
        }

        public void SetValue(object target, object value)
        {
            // Not needed for serialization
        }
    }
}
