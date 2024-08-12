#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace ninaAPI.WebService
{
    public static class CommonErrors
    {
        public static readonly Error MISSING_API_KEY = new Error("API Key is missing in the header", 401);
        public static readonly Error INVALID_API_KEY = new Error("API Key is not valid", 401);
        public static readonly Error PROPERTY_NOT_SEND = new Error("Property was not send", 400);
        public static readonly Error INVALID_PROPERTY = new Error("Property is not valid", 400);
        public static readonly Error INDEX_OUT_OF_RANGE = new Error("Index out of range", 400);
        public static readonly Error UNKNOWN_ERROR = new Error("Unknown error", 500);
        public static readonly Error UNKNOWN_ACTION = new Error("Unknown action", 400);
    }

    public class Error
    {
        public string message;
        public int code;

        public Error(string Message, int Code)
        {
            message = Message;
            code = Code;
        }
    }
}
