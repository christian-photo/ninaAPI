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
        public const string CAMERA_DOWNLOAD_TIMEOUT = "CameraDownloadTimeout";

        public const string DOME_SHUTTER_OPENED = "DomeShutterOpened";
        public const string DOME_SHUTTER_CLOSED = "DomeShutterClosed";
        public const string DOME_HOMED = "DomeHomed";
        public const string DOME_SLEWED = "DomeSlewed";
        public const string DOME_PARKED = "DomeParked";
        public const string DOME_SYNCED = "DomeSynced";

        public const string FILTERWHEEL_FILTER_CHANGED = "FilterwheelFilterChanged";

        public const string FLATDEVICE_LIGHT_TOGGLED = "FlatdeviceLightToggled";
        public const string FLATDEVICE_OPENED = "FlatdeviceOpened";
        public const string FLATDEVICE_CLOSED = "FlatdeviceClosed";
        public const string FLATDEVICE_BRIGHTNESS_CHANGED = "FlatdeviceBrightnessChanged";

        public const string FOCUSER_USER_FOCUSED = "UserFocused";
        public const string FOCUSER_NEW_AF_POINT = "NewAutofocusPoint";
        public const string FOCUSER_AF_ENDED = "AutofocusEnded";
        public const string FOCUSER_AF_STARTED = "AutofocusStarted";

        public const string GUIDER_DITHER = "Dither";
        public const string GUIDER_GUIDING_STARTED = "GuidingStarted";
        public const string GUIDER_GUIDING_STOPPED = "GuidingStopped";
        public const string GUIDER_GUIDESTEP = "Guidestep";

        public const string MOUNT_FLIP_FINISHED = "MountFlipFinished";
        public const string MOUNT_FLIP_STARTED = "MountFlipStarted";
        public const string MOUNT_HOMED = "MountHomed";
        public const string MOUNT_SLEWED = "MountSlewed";
        public const string MOUNT_PARKED = "MountParked";
        public const string MOUNT_UNPARKED = "MountUnparked";

        public const string ROTATOR_MOVED = "RotatorMoved";
        public const string ROTATOR_SYNCED = "RotatorSynced";

        public const string SAFETYMONITOR_SAFETY_CHANGED = "SafetymonitorSafetyChanged";

        public const string PROCESS_STARTED = "ProcessStarted";
        public const string PROCESS_FINISHED = "ProcessFinished";

        public const string IMAGE_SAVED = "ImageSaved";
        public const string IMAGE_PREPARED = "ImagePrepared";

        public const string PROFILE_ADDED = "ProfileAdded";
        public const string PROFILE_CHANGED = "ProfileChanged";
        public const string PROFILE_REMOVED = "ProfileRemoved";

        public const string SEQUENCE_ENTITY_FAILED = "SequenceEntityFailed";
        public const string SEQUENCE_CUSTOM_EVENT = "SequenceCustomEvent";
        public const string SEQUENCE_STARTED = "SequenceStarted";
        public const string SEQUENCE_FINISHED = "SequenceFinished";

        public const string LIVESTACK_STATUS = "LivestackStatusUpdate";
        public const string LIVESTACK_STACK_UPDATED = "LivestackStackUpdated";

        public const string TPPA_ALIGNMENT_ERROR = "TppaAlignmentError";
        public const string TPPA_PROGRESS_UPDATE = "TppaProgressUpdate";

        public const string TS_WAITSTART = "TsWaitStarted";
        public const string TS_NEWTARGETSTART = "TsNewtargetStarted";
        public const string TS_TARGETSTART = "TsTargetStarted";
        public const string TS_STOP = "TsContainerStopped";
        public const string TS_COMPLETE = "TsTargetComplete";

        public static string DeviceConnected(Device device) => $"{device.ToString().ToLower()}Connected";
        public static string DeviceDisconnected(Device device) => $"{device.ToString().ToLower()}Disconnected";
        public static string DeviceInfoUpdate(Device device) => $"{device.ToString().ToLower()}InfoUpdate";
    }
}
