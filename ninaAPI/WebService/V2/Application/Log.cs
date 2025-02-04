#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


namespace ninaAPI.WebService.V2
{
    public class NinaLogWatcher : INinaWatcher
    {
        private NINALogMessageProcessor LogProcessor;
        private NINALogWatcher LogWatcher;

        public void StartWatchers()
        {
            LogProcessor = new NINALogMessageProcessor();
            LogWatcher = new NINALogWatcher(LogProcessor);
            LogWatcher.Start();

            LogProcessor.NINALogEventSaved += LogEvent;
        }

        public void StopWatchers()
        {
            LogProcessor.NINALogEventSaved -= LogEvent;
            LogWatcher?.Stop();
        }

        private async void LogEvent(object _, NINALogEvent e) => await WebSocketV2.SendAndAddEvent(e.type, e.time);
    }
}
