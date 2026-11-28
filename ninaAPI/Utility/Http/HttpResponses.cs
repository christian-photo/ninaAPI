#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Net;

namespace ninaAPI.Utility.Http
{
    public class CustomResponse
    {
        public const string TypeAPI = "API";
        public const string TypeSocket = "Socket";

        public object Response { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 200;
        public bool Success { get; set; } = true;
        public string Type { get; set; } = TypeAPI;
    }

    public class StringResponse(string message)
    {
        public string Message { get; set; } = message;
    }

    public class StatusResponse(ApiProcessStatus status)
    {
        public ApiProcessStatus Status { get; set; } = status;
    }

    public class ResponseFactory
    {
        public static object CreateProcessResponse(ApiProcessStartResult result, Guid id)
        {
            return new { Status = result.ToString(), ProcessId = id };
        }

        public static object CreateProcessConflictsResponse(ApiProcessMediator mediator, ApiProcess process)
        {
            var conflicts = mediator.CheckForConflicts(process.ProcessType, process.ProcessId);
            return new
            {
                Error = HttpUtility.StatusCodeMessages[(int)HttpStatusCode.Conflict],
                Message = "Process could not be started because other processes conflict with it",
                Conflicts = conflicts
            };
        }

        public static (object, int) CreateProcessStartedResponse(ApiProcessStartResult result, ApiProcessMediator mediator, ApiProcess process)
        {
            object response;
            int statusCode = 202;

            if (result == ApiProcessStartResult.Conflict)
            {
                response = CreateProcessConflictsResponse(mediator, process);
                statusCode = (int)HttpStatusCode.Conflict;
            }
            else
            {
                response = CreateProcessResponse(result, process.ProcessId);
            }

            return (response, statusCode);
        }
    }
}
