#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace ninaAPI.WebService
{
    public static class CommonErrors
    {
        public static readonly Error INDEX_OUT_OF_RANGE = new Error("Index out of range", 400);
        public static readonly Error UNKNOWN_ERROR = new Error("Unknown error", 500);
    }

    public class Error(string Message, int Code)
    {
        public string message = Message;
        public int code = Code;
    }
}
