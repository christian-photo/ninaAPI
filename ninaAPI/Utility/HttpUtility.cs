using System;
using System.Linq;
using EmbedIO;
using ninaAPI.WebService;

namespace ninaAPI.Utility
{
    public static class HttpUtility
    {
        public static bool IsParameterOmitted(this IHttpContext context, string parameter)
        {
            return !context.Request.QueryString.AllKeys.Contains(parameter);
        }

        public static T GetParameter<T>(this IHttpContext context, string parameter, T defaultValue, bool required = false)
        {
            if (context.IsParameterOmitted(parameter))
            {
                if (required)
                {
                    throw CommonErrors.ParameterMissing(parameter);
                }
                return defaultValue;
            }

            var value = context.Request.QueryString.Get(parameter);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    throw CommonErrors.ParameterMissing(parameter);
                }
                return defaultValue;
            }

            object result = value.CastString(typeof(T));
            if (result is T t)
                return t;

            // Try to convert if CastString didn't return the right type
            return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}