using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


// https://github.com/tcpalmer/nina.plugin.web/blob/main/NINA.Plugin.Web/NINA.Plugin.Web/LogEvent/NINALogMessageProcessor.cs (modified)
namespace ninaAPI
{
    public class NINALogMessageProcessor
    {
        private static List<NINALogEvent> previousEvents = new List<NINALogEvent>();
        private Dictionary<Regex, EventMatcher> matchers;

        public NINALogMessageProcessor()
        {
            matchers = initMatchers();
        }

        public void processLogMessages(List<string> messages)
        {
            Logger.Trace($"processing {messages.Count} new log messages");

            foreach (var line in messages)
            {

                if (filterMessage(line))
                {
                    continue;
                }

                string[] parts = line.Split('|');
                if (parts.Length == 6)
                {
                    string message = parts[5];

                    foreach (Regex regex in matchers.Keys)
                    {
                        Match match = regex.Match(message);
                        if (match.Success)
                        {
                            EventMatcher eventMatcher = matchers[regex];
                            DateTime dateTime = DateTime.Parse(parts[0]);
                            NINALogEvent logEvent = eventMatcher.handleMessage(eventMatcher, message, dateTime, match);
                            if (logEvent != null)
                            {
                                onNINALogEvent(logEvent);
                            }
                        }
                    }
                }
                else
                {
                    Logger.Trace($"log message is not regular form: {line}, skipping");
                }
            }
        }

        public event EventHandler<NINALogEvent> NINALogEventSaved;

        public void onNINALogEvent(NINALogEvent e)
        {
            previousEvents.Add(e);
            Logger.Debug($"detected event for web viewer: {e.type}");
            NINALogEventSaved?.Invoke(this, e);
        }

        private Dictionary<Regex, EventMatcher> initMatchers()
        {
            Dictionary<Regex, EventMatcher> _matchers = new Dictionary<Regex, EventMatcher>();

            RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase;

            // Dome stopped
            Regex re = new Regex("^Stopping all dome movement$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_DOME_STOPPED, false, null));

            // Center / Plate solve in Sequence
            re = new Regex("^Starting Category: Telescope, Item: Center, (?<extra>.+)$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_CENTER, true, null));

            // Error: Auto Focus
            re = new Regex("^Autofocus failed to run(?<extra>.+)$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_ERROR_AF, true, null));

            // Error: Plate solve
            re = new Regex("^ASTAP - Plate solve failed.", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_ERROR_PLATESOLVE, false, null));

            return _matchers;
        }

        private bool filterMessage(string line)
        {
            if (line?.Length == 0)
            {
                return true;
            }

            if (line.Contains("|DEBUG|") || line.Contains("|TRACE|"))
            {
                return true;
            }

            return false;
        }

        public delegate NINALogEvent HandleMessageDelegate(EventMatcher eventMatcher, string msg, DateTime dateTime, Match match);

        public class EventMatcher
        {
            public string eventType { get; }
            public bool hasExtra { get; }
            public HandleMessageDelegate customMessageHandler;

            public EventMatcher(string eventType, bool hasExtra, HandleMessageDelegate handleMessage)
            {
                this.eventType = eventType;
                this.hasExtra = hasExtra;
                this.customMessageHandler = handleMessage;
            }

            public NINALogEvent handleMessage(EventMatcher eventMatcher, string msg, DateTime dateTime, Match match)
            {
                if (customMessageHandler != null)
                {
                    return customMessageHandler(eventMatcher, msg, dateTime, match);
                }

                if (eventMatcher.hasExtra && match.Groups.Count > 0)
                {
                    return new NINALogEvent(eventMatcher.eventType, dateTime, match.Groups["extra"].Value);
                }
                else
                {
                    return new NINALogEvent(eventMatcher.eventType, dateTime);
                }
            }
        }
    }
}