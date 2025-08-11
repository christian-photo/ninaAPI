using System;


// https://github.com/tcpalmer/nina.plugin.web/blob/main/NINA.Plugin.Web/NINA.Plugin.Web/LogEvent/NINALogEvent.cs (modified)
namespace ninaAPI
{
    public class NINALogEvent
    {
        public const string NINA_DOME_STOPPED = "DOME-STOPPED";
        public const string NINA_CENTER = "MOUNT-CENTER";
        public const string NINA_ERROR_AF = "ERROR-AF";
        public const string NINA_ERROR_PLATESOLVE = "ERROR-PLATESOLVE";

        public string id { get; set; }
        public string type { get; set; }
        public DateTime time { get; set; }
        public string extra { get; set; }

        public NINALogEvent()
        {
        }

        public NINALogEvent(string type) : this(type, DateTime.Now, null)
        {
        }

        public NINALogEvent(string type, DateTime dateTime) : this(type, dateTime, null)
        {
        }

        public NINALogEvent(string type, DateTime dateTime, string extra)
        {
            this.id = Guid.NewGuid().ToString();
            this.type = type;
            this.time = dateTime;
            this.extra = extra;
        }

        public override string ToString()
        {
            return (extra != null) ? $"time: {time}, type: {type}, extra: {extra}" : $"time: {time}, type: {type}";
        }
    }
}