#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        private static NINALogMessageProcessor LogProcessor;
        private static NINALogWatcher LogWatcher;

        public static void StartLogWatcher()
        {
            LogProcessor = new NINALogMessageProcessor();
            LogWatcher = new NINALogWatcher(LogProcessor);
            LogWatcher.Start();

            LogProcessor.NINALogEventSaved += LogEvent;
        }

        public static void StopLogWatcher()
        {
            LogProcessor.NINALogEventSaved -= LogEvent;
            LogWatcher?.Stop();
        }

        private static async void LogEvent(object _, NINALogEvent e) => await WebSocketV2.SendAndAddEvent(e.type, e.time);
    }
}
