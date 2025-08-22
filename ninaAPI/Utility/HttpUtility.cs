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
using EmbedIO;
using ninaAPI.WebService;
using ninaAPI.WebService.Interfaces;

namespace ninaAPI.Utility
{
    public static class HttpUtility
    {
        public static bool IsParameterOmitted(this IHttpContext context, string parameter)
        {
            return !context.Request.QueryString.AllKeys.Contains(parameter);
        }

        public static bool IsParameterOmitted<T>(this IHttpContext context, IQueryParameter<T> parameter)
        {
            return !context.Request.QueryString.AllKeys.Contains(parameter.ParameterName);
        }
    }

    public class QueryParameter<T> : IQueryParameter<T>
    {
        public string ParameterName { get; }
        public T DefaultValue { get; }
        public bool Required { get; }

        public bool WasProvided { get; set; }
        public T Value { get; set; }

        public T Get(IHttpContext context)
        {
            if (context.IsParameterOmitted(this))
            {
                WasProvided = false;
                if (Required)
                {
                    throw CommonErrors.ParameterMissing(ParameterName);
                }
                Value = DefaultValue;
                return Value;
            }

            var value = context.Request.QueryString.Get(ParameterName);
            if (string.IsNullOrWhiteSpace(value))
            {
                WasProvided = false;
                if (Required)
                {
                    throw CommonErrors.ParameterMissing(ParameterName);
                }
                Value = DefaultValue;
                return Value;
            }

            WasProvided = true;

            object result = value.CastString(typeof(T));
            if (result is T t)
            {
                Value = t;
            }
            else
            {
                // Try to convert if CastString didn't return the right type
                Value = (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            }

            return Value;
        }

        public QueryParameter(string parameterName, T defaultValue, bool required = false)
        {
            ParameterName = parameterName;
            DefaultValue = defaultValue;
            Required = required;
        }
    }

    public class SizeQueryParameter : QueryParameter<Size>
    {
        public new Size Get(IHttpContext context)
        {
            int width = widthParam.Get(context);
            int height = heightParam.Get(context);

            Value = new Size(width, height);
            WasProvided = widthParam.WasProvided && heightParam.WasProvided;
            return Value;
        }

        private QueryParameter<int> widthParam;
        private QueryParameter<int> heightParam;

        public SizeQueryParameter(Size size, bool required) : base("CONTAINER_PARAM", new Size(0, 0), false)
        {
            widthParam = new QueryParameter<int>("width", size.Width, required);
            heightParam = new QueryParameter<int>("height", size.Height, required);
        }
    }

    public class ImageQueryParameterSet
    {
        public SizeQueryParameter Size { get; set; }
        public QueryParameter<bool> Resize { get; set; }
        public QueryParameter<float> Scale { get; set; }
        public QueryParameter<int> Quality { get; set; }
        public QueryParameter<float> ROI { get; set; }
        public QueryParameter<float> StretchFactor { get; set; }

        private ImageQueryParameterSet()
        {
        }

        public static ImageQueryParameterSet Default()
        {
            return new ImageQueryParameterSet()
            {
                Size = new SizeQueryParameter(new Size(1500, 1000), false),
                Resize = new QueryParameter<bool>("resize", false, false),
                Scale = new QueryParameter<float>("scale", 0.5f, false),
                Quality = new QueryParameter<int>("quality", -1, false),
                ROI = new QueryParameter<float>("roi", 1.0f, false),
                StretchFactor = new QueryParameter<float>("stretchFactor", 1.0f, false),
            };
        }

        public void Evaluate(IHttpContext context)
        {
            Size.Get(context);
            Resize.Get(context);
            Scale.Get(context);
            Quality.Get(context);
            ROI.Get(context);
            StretchFactor.Get(context);
        }
    }
}