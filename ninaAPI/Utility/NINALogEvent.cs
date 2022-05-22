using System;


// https://github.com/tcpalmer/nina.plugin.web/blob/main/NINA.Plugin.Web/NINA.Plugin.Web/LogEvent/NINALogEvent.cs (modified)
namespace ninaAPI
{
    public class NINALogEvent
    {
        public const string NINA_PARK = "NINA-PARK";
        public const string NINA_UNPARK = "NINA-UNPARK";
        public const string NINA_DOME_SHUTTER_OPENED = "NINA-DOME-SHUTTER-OPENED";
        public const string NINA_DOME_SHUTTER_CLOSED = "NINA-DOME-SHUTTER-CLOSED";
        public const string NINA_DOME_STOPPED = "NINA-DOME-STOPPED";
        public const string NINA_ADV_SEQ_START = "NINA-ADV-SEQ-START";
        public const string NINA_ADV_SEQ_STOP = "NINA-ADV-SEQ-STOP";
        public const string NINA_AF = "NINA-AF";
        public const string NINA_CENTER = "NINA-CENTER";
        public const string NINA_SLEW = "NINA-SLEW";
        public const string NINA_MF = "NINA-MF";
        public const string NINA_ERROR_AF = "NINA-ERROR-AF";
        public const string NINA_ERROR_PLATESOLVE = "NINA-ERROR-PLATESOLVE";

        
        public const string NINA_CAMERA_CONNECTION_CHANGED = "NINA-CAMERA-CONNECTION-CHANGED";
        public const string NINA_TELESCOPE_CONNECTION_CHANGED = "NINA-TELESCOPE-CONNECTION-CHANGED";
        public const string NINA_FOCUSER_CONNECTION_CHANGED = "NINA-FOCUSER-CONNECTION-CHANGED";
        public const string NINA_FILTER_CONNECTION_CHANGED = "NINA-FILTER-CONNECTION-CHANGED";
        public const string NINA_ROTATOR_CONNECTION_CHANGED = "NINA-ROTATOR-CONNECTION-CHANGED";
        public const string NINA_SWITCH_CONNECTION_CHANGED = "NINA-SWITCH-CONNECTION-CHANGED";
        public const string NINA_WEATHER_CONNECTION_CHANGED = "NINA-WEATHER-CONNECTION-CHANGED";
        public const string NINA_DOME_CONNECTION_CHANGED = "NINA-DOME-CONNECTION-CHANGED";
        public const string NINA_SAFETY_CONNECTION_CHANGED = "NINA-SAFETY-CONNECTION-CHANGED";

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