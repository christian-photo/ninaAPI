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
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using EmbedIO;
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using ninaAPI.WebService;
using ninaAPI.WebService.Interfaces;

namespace ninaAPI.Utility.Http
{
    public static class HttpUtility
    {
        public static bool IsParameterOmitted(this IHttpContext context, string parameter)
        {
            var keys = context?.Request?.QueryString?.AllKeys;
            if (keys == null || keys.Length == 0) return true;
            return !keys.Any(k => string.Equals(k, parameter, StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool IsParameterOmitted<T>(this IHttpContext context, IQueryParameter<T> parameter)
        {
            return context.IsParameterOmitted(parameter.ParameterName);
        }

        public static readonly Dictionary<int, string> StatusCodeMessages = new Dictionary<int, string>()
        {
            { 400, "Bad Request" },
            { 401, "Unauthorized" },
            { 403, "Forbidden" },
            { 404, "Not Found" },
            { 405, "Method Not Allowed" },
            { 406, "Not Acceptable" },
            { 408, "Request Timeout" },
            { 409, "Conflict" },
            { 422, "Unprocessable Entity" },
            { 423, "Locked" },
            { 429, "Too Many Requests" },
            { 500, "Internal Server Error" },
            { 501, "Not Implemented" },
            { 502, "Bad Gateway" },
            { 503, "Service Unavailable" },
            { 504, "Gateway Timeout" },
            { 507, "Insufficient Storage" },
        };
    }

    public class QueryParameter<T> : IQueryParameter<T>
    {
        public string ParameterName { get; }
        public T DefaultValue { get; }
        public bool Required { get; }
        private Func<T, bool> Validate;

        public bool WasProvided { get; set; }
        public T Value { get; set; }

        private T Evaluate(IHttpContext context)
        {
            if (context.IsParameterOmitted(this))
            {
                WasProvided = false;
                if (Required)
                    throw CommonErrors.ParameterMissing(ParameterName);
                return DefaultValue;
            }

            var raw = context.Request.QueryString.Get(ParameterName);
            if (string.IsNullOrWhiteSpace(raw))
            {
                WasProvided = false;
                if (Required)
                    throw CommonErrors.ParameterMissing(ParameterName);
                return DefaultValue;
            }

            WasProvided = true;

            // determine target (handle Nullable<T>)
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            try
            {
                object converted;

                // prefer existing helper if it reliably converts simple types
                // fallback to robust conversion logic:
                if (targetType.IsEnum)
                {
                    converted = Enum.Parse(targetType, raw, ignoreCase: true);
                }
                else if (targetType == typeof(Guid))
                {
                    converted = Guid.Parse(raw);
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(targetType);
                    if (converter != null && converter.CanConvertFrom(typeof(string)))
                        converted = converter.ConvertFromInvariantString(raw);
                    else
                        converted = Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
                }

                return (T)converted;
            }
            catch
            {
                throw CommonErrors.ParameterInvalid(ParameterName);
            }
        }

        public T Get(IHttpContext context)
        {
            Value = Evaluate(context);
            if (Validate(Value))
            {
                return Value;
            }
            else
            {
                throw CommonErrors.ParameterInvalid(ParameterName);
            }
        }

        public QueryParameter(string parameterName, T defaultValue, bool required = false, Func<T, bool> validate = null)
        {
            ParameterName = parameterName;
            DefaultValue = defaultValue;
            Required = required;
            Validate = validate is null ? (v => true) : validate;
        }
    }

    public class SizeQueryParameter : QueryParameter<Size>
    {
        public new Size Get(IHttpContext context)
        {
            int width = widthParam.Get(context);
            int height = heightParam.Get(context);

            if (widthParam.WasProvided ^ heightParam.WasProvided)
            {
                // One of both was set, but not both
                if (allowOneSide)
                {
                    Value = new Size(
                        widthParam.WasProvided ? width : 0,
                        heightParam.WasProvided ? height : 0
                    );
                    WasProvided = true;
                    return Value;
                }
                else if (!required)
                {
                    Value = DefaultValue;
                    WasProvided = false;
                    return Value;
                }
                else
                {
                    throw CommonErrors.ParameterMissing("height or width");
                }
            }
            else if (!(widthParam.WasProvided && heightParam.WasProvided))
            {
                // None were set
                if (required)
                {
                    throw CommonErrors.ParameterMissing("width and height");
                }
                else
                {
                    Value = DefaultValue;
                    WasProvided = false;
                    return Value;
                }
            }
            else
            {
                Value = new Size(width, height);
                WasProvided = true;
                return Value;
            }
        }

        private QueryParameter<int> widthParam;
        private QueryParameter<int> heightParam;

        private bool allowOneSide;
        private bool required;

        public SizeQueryParameter(Size size, bool required, bool allowOneSide = true, string widthName = "width", string heightName = "height") : base("CONTAINER_PARAM", size, false)
        {
            widthParam = new QueryParameter<int>(widthName, size.Width, false, (width) => width > 0);
            heightParam = new QueryParameter<int>(heightName, size.Height, false, (height) => height > 0);

            this.allowOneSide = allowOneSide;
            this.required = required;
        }
    }

    public class ImageQueryParameterSet
    {
        public SizeQueryParameter Size { get; set; }
        public QueryParameter<bool> Resize { get; set; }
        public QueryParameter<float> Scale { get; set; }
        public QueryParameter<int> Quality { get; set; }
        public QueryParameter<double> StretchFactor { get; set; }
        public QueryParameter<bool> Debayer { get; set; }
        public QueryParameter<SensorType> BayerPattern { get; set; }
        public QueryParameter<bool> UnlinkedStretch { get; set; }
        public QueryParameter<double> BlackClipping { get; set; }
        public QueryParameter<RawConverterEnum> RawConverter { get; set; }


        private ImageQueryParameterSet()
        {
        }

        public static ImageQueryParameterSet Default()
        {
            return new ImageQueryParameterSet()
            {
                Size = new SizeQueryParameter(new Size(1500, 1000), false),
                Resize = new QueryParameter<bool>("resize", false, false),
                Scale = new QueryParameter<float>("scale", 0.5f, false, (scale) => scale > 0 && scale <= 1),
                Quality = new QueryParameter<int>("quality", -1, false, (quality) => quality >= -1 && quality <= 100),
                StretchFactor = new QueryParameter<double>("stretch-factor", 1.0f, false),
                Debayer = new QueryParameter<bool>("debayer", false, false),
                BayerPattern = new QueryParameter<SensorType>("bayer-pattern", SensorType.Monochrome, false),
                UnlinkedStretch = new QueryParameter<bool>("unlinked-stretch", false, false),
                BlackClipping = new QueryParameter<double>("black-clipping", 0.0, false),
                RawConverter = new QueryParameter<RawConverterEnum>("raw-converter", RawConverterEnum.FREEIMAGE, false),
            };
        }

        public static ImageQueryParameterSet ByProfile(IProfile profile)
        {
            var set = Default();
            set.StretchFactor = new QueryParameter<double>("stretch-factor", profile.ImageSettings.AutoStretchFactor, false);
            set.Debayer = new QueryParameter<bool>("debayer", profile.ImageSettings.DebayerImage, false);
            set.BlackClipping = new QueryParameter<double>("black-clipping", profile.ImageSettings.BlackClipping, false);
            set.UnlinkedStretch = new QueryParameter<bool>("unlinked-stretch", profile.ImageSettings.UnlinkedStretch, false);
            set.RawConverter = new QueryParameter<RawConverterEnum>("raw-converter", profile.CameraSettings.RawConverter, false);
            return set;
        }

        public void Evaluate(IHttpContext context)
        {
            Size.Get(context);
            Resize.Get(context);
            Scale.Get(context);
            Quality.Get(context);
            StretchFactor.Get(context);
            RawConverter.Get(context);
            Debayer.Get(context);
            BayerPattern.Get(context);
            UnlinkedStretch.Get(context);
            BlackClipping.Get(context);
        }
    }
}