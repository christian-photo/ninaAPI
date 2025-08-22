#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Net;
using EmbedIO;
using ninaAPI.Utility;

namespace ninaAPI.WebService
{
    public interface INinaWatcher
    {
        public void StartWatchers();
        public void StopWatchers();
    }

    public static class CommonErrors
    {
        public static readonly Error INDEX_OUT_OF_RANGE = new Error("Index out of range", 400);
        public static readonly Error UNKNOWN_ERROR = new Error("Unknown error", 500);

        public static HttpException DeviceNotConnected(Device device) => new(HttpStatusCode.Conflict, $"{device} is not connected");
        public static HttpException ProcessAlreadyRunning() => new(HttpStatusCode.Conflict, "Process already running");
        public static HttpException ParameterMissing(string parameter) => new(HttpStatusCode.BadRequest, $"Parameter {parameter} is missing");
        public static HttpException ParameterInvalid(string parameter) => new(HttpStatusCode.BadRequest, $"Parameter {parameter} is invalid");
        public static HttpException ParameterFormatInvalid(string parameter, string format) => new(HttpStatusCode.BadRequest, $"Parameter {parameter} is in an invalid format, expected {format}");
        public static HttpException UnknwonError() => new(HttpStatusCode.InternalServerError, "Unknown error");
    }

    public class Error(string Message, int Code)
    {
        public string message = Message;
        public int code = Code;
    }
}