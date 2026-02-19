#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ninaAPI.Utility;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public static class WebSocketEvents
    {
        public const string CAMERA_DOWNLOAD_TIMEOUT = "camera-download-timeout";

        public const string DOME_SHUTTER_OPENED = "dome-shutter-opened";
        public const string DOME_SHUTTER_CLOSED = "dome-shutter-closed";
        public const string DOME_HOMED = "dome-homed";
        public const string DOME_SLEWED = "dome-slewed";
        public const string DOME_PARKED = "dome-parked";
        public const string DOME_SYNCED = "dome-synced";

        public const string FILTERWHEEL_FILTER_CHANGED = "filterwheel-filter-changed";

        public const string FLATDEVICE_LIGHT_TOGGLED = "flatdevice-light-toggled";
        public const string FLATDEVICE_OPENED = "flatdevice-opened";
        public const string FLATDEVICE_CLOSED = "flatdevice-closed";
        public const string FLATDEVICE_BRIGHTNESS_CHANGED = "flatdevice-brightness-changed";

        public const string FOCUSER_USER_FOCUSED = "user-focused";
        public const string FOCUSER_NEW_AF_POINT = "autofocus-point";
        public const string FOCUSER_AF_ENDED = "autofocus-ended";
        public const string FOCUSER_AF_STARTED = "autofocus-started";

        public const string GUIDER_DITHER = "guider-dither";
        public const string GUIDER_GUIDING_STARTED = "guider-guiding-started";
        public const string GUIDER_GUIDING_STOPPED = "guider-guiding-stopped";
        public const string GUIDER_GUIDESTEP = "guider-guidestep";

        public const string MOUNT_FLIP_FINISHED = "mount-flip-finished";
        public const string MOUNT_FLIP_STARTED = "mount-flip-started";
        public const string MOUNT_HOMED = "mount-homed";
        public const string MOUNT_SLEWED = "mount-slewed";
        public const string MOUNT_PARKED = "mount-parked";
        public const string MOUNT_UNPARKED = "mount-unparked";

        public const string ROTATOR_MOVED = "rotator-moved";
        public const string ROTATOR_SYNCED = "rotator-synced";

        public const string SAFETYMONITOR_SAFETY_CHANGED = "safetymonitor-safety-changed";

        public const string PROCESS_STARTED = "process-started";
        public const string PROCESS_FINISHED = "process-finished";

        public const string IMAGE_SAVED = "image-saved";
        public const string IMAGE_PREPARED = "image-prepared";

        public const string PROFILE_ADDED = "profile-added";
        public const string PROFILE_CHANGED = "profile-changed";
        public const string PROFILE_REMOVED = "profile-removed";

        public const string SEQUENCE_ENTITY_FAILED = "sequence-entity-failed";
        public const string SEQUENCE_CUSTOM_EVENT = "sequence-custom-event";
        public const string SEQUENCE_STARTED = "sequence-started";
        public const string SEQUENCE_FINISHED = "sequence-finished";

        public static string DeviceConnected(Device device) => $"{device.ToString().ToLower()}-connected";
        public static string DeviceDisconnected(Device device) => $"{device.ToString().ToLower()}-disconnected";
        public static string DeviceInfoUpdate(Device device) => $"{device.ToString().ToLower()}-info-update";
    }
}
