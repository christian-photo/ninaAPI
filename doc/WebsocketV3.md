# Websocket V3

The plugin ships 2 different websockets:

- [Event Websocket](WebsocketV3.md#event-websocket): `ws://localhost:1888/v3/ws/events`
- [Mount Control Websocket](WebsocketV3.md#mount-control-websocket): `ws://localhost:1888/v3/ws/mount-control`

> [!IMPORTANT]
> If SSL enabled the protocol switches to `wss://`. Also, websockets too require basic auth if it is enabled

## Event Websocket

The event websocket is used to receive events from NINA. The events are sent as JSON objects and separated into several channels.
You can choose to subscribe to or unscribe from a channel by sending a `Subscribe` or `Unsubscribe` message to the websocket.
By default, you are subscribed to all channels **except** the `<device>InfoUpdate` channels.

### Format

Events have the following format:

```json
{
  "Event": "<Event Name>",
  "Data": {
    "AdditionalData": "Data"
  }
}
```

Required is only the `Event` field. The `Data` field is optional and can contain supplementary data.

### Events

There are in total 82 distinct events:

| Event                         | Channel                   | Description                                 | Additional Data                                                                                                            |
| ----------------------------- | ------------------------- | ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `CameraConnected`             | `Equipment`               | Camera connected                            | None                                                                                                                       |
| `CameraDisconnected`          | `Equipment`               | Camera disconnected                         | None                                                                                                                       |
| `CameraDownloadTimeout`       | `Equipment`               | Camera timed out while downloading an image | None                                                                                                                       |
| `CameraInfoUpdate`            | `CameraInfoUpdate`        | Camera info updated                         | [Camera Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/CameraInfo)                  |
| `DomeConnected`               | `Equipment`               | Dome connected                              | None                                                                                                                       |
| `DomeDisconnected`            | `Equipment`               | Dome disconnected                           | None                                                                                                                       |
| `DomeShutterOpened`           | `Equipment`               | Dome shutter opened                         | None                                                                                                                       |
| `DomeShutterClosed`           | `Equipment`               | Dome shutter closed                         | None                                                                                                                       |
| `DomeHomed`                   | `Equipment`               | Dome homed                                  | None                                                                                                                       |
| `DomeSlewed`                  | `Equipment`               | Dome slewed                                 | `From`, `To`                                                                                                               |
| `DomeParked`                  | `Equipment`               | Dome parked                                 | None                                                                                                                       |
| `DomeSynced`                  | `Equipment`               | Dome synced                                 | None                                                                                                                       |
| `DomeInfoUpdate`              | `DomeInfoUpdate`          | Dome info updated                           | [Dome Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/DomeInfo)                      |
| `FilterwheelConnected`        | `Equipment`               | Filterwheel connected                       | None                                                                                                                       |
| `FilterwheelDisconnected`     | `Equipment`               | Filterwheel disconnected                    | None                                                                                                                       |
| `FilterwheelFilterChanged`    | `Equipment`               | Filterwheel filter changed                  | `From` (FilterData), `To` (FilterData)                                                                                     |
| `FilterwheelInfoUpdate`       | `FilterwheelInfoUpdate`   | Filterwheel info updated                    | [Filterwheel Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/FWInfo)                 |
| `FlatdeviceConnected`         | `Equipment`               | Flatdevice connected                        | None                                                                                                                       |
| `FlatdeviceDisconnected`      | `Equipment`               | Flatdevice disconnected                     | None                                                                                                                       |
| `FlatdeviceLightToggled`      | `Equipment`               | Flatdevice light toggled                    | None                                                                                                                       |
| `FlatdeviceOpened`            | `Equipment`               | Flatdevice opened                           | None                                                                                                                       |
| `FlatdeviceClosed`            | `Equipment`               | Flatdevice closed                           | None                                                                                                                       |
| `FlatdeviceBrightnessChanged` | `Equipment`               | Flatdevice brightness changed               | `From`, `To`                                                                                                               |
| `FlatdeviceInfoUpdate`        | `FlatdeviceInfoUpdate`    | Flatdevice info updated                     | [Flatdevice Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/FlatDeviceInfo)          |
| `FocuserConnected`            | `Equipment`               | Focuser connected                           | None                                                                                                                       |
| `FocuserDisconnected`         | `Equipment`               | Focuser disconnected                        | None                                                                                                                       |
| `UserFocused`                 | `Autofocus`               | User focused                                | None                                                                                                                       |
| `NewAutofocusPoint`           | `Autofocus`               | New autofocus point                         | `X`, `Y`                                                                                                                   |
| `AutofocusStarted`            | `Autofocus`               | Autofocus started                           | None                                                                                                                       |
| `AutofocusEnded`              | `Autofocus`               | Autofocus ended                             | `Filter`, `Timestamp`, `Temperature`, `Position`                                                                           |
| `FocuserInfoUpdate`           | `FocuserInfoUpdate`       | Focuser info updated                        | [Focuser Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/FocuserInfo)                |
| `GuiderConnected`             | `Equipment`               | Guider connected                            | None                                                                                                                       |
| `GuiderDisconnected`          | `Equipment`               | Guider disconnected                         | None                                                                                                                       |
| `Dither`                      | `Guiding`                 | Dither                                      | None                                                                                                                       |
| `GuidingStarted`              | `Equipment`               | Guiding started                             | None                                                                                                                       |
| `GuidingStopped`              | `Equipment`               | Guiding stopped                             | None                                                                                                                       |
| `Guidestep`                   | `Guiding`                 | Guide step                                  | `RADistanceRaw`, `DECDistanceRaw`, `RADuration`, `DECDuration`                                                             |
| `GuiderInfoUpdate`            | `GuiderInfoUpdate`        | Guider info updated                         | [Guider Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/GuiderInfo)                  |
| `MountConnected`              | `Equipment`               | Mount connected                             | None                                                                                                                       |
| `MountDisconnected`           | `Equipment`               | Mount disconnected                          | None                                                                                                                       |
| `MountFlipStarted`            | `Equipment`               | Mount flip started                          | `TargetCoordinates`                                                                                                        |
| `MountFlipFinished`           | `Equipment`               | Mount flip finished                         | `TargetCoordinates`, `Success`                                                                                             |
| `MountHomed`                  | `Equipment`               | Mount homed                                 | None                                                                                                                       |
| `MountSlewed`                 | `Equipment`               | Mount slewed                                | `From`, `To`                                                                                                               |
| `MountParked`                 | `Equipment`               | Mount parked                                | None                                                                                                                       |
| `MountUnparked`               | `Equipment`               | Mount unparked                              | None                                                                                                                       |
| `MountInfoUpdate`             | `MountInfoUpdate`         | Mount info updated                          | [Mount Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/MountInfo)                    |
| `RotatorConnected`            | `Equipment`               | Rotator connected                           | None                                                                                                                       |
| `RotatorDisconnected`         | `Equipment`               | Rotator disconnected                        | None                                                                                                                       |
| `RotatorMoved`                | `Equipment`               | Rotator moved                               | `From`, `To`, `Mechanical`                                                                                                 |
| `RotatorSynced`               | `Equipment`               | Rotator synced                              | `From`, `To`                                                                                                               |
| `RotatorInfoUpdate`           | `RotatorInfoUpdate`       | Rotator info updated                        | [Rotator Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/RotatorInfo)                |
| `SafetymonitorConnected`      | `Equipment`               | Safetymonitor connected                     | None                                                                                                                       |
| `SafetymonitorDisconnected`   | `Equipment`               | Safetymonitor disconnected                  | None                                                                                                                       |
| `SafetymonitorSafetyChanged`  | `Equipment`               | Safetymonitor safety changed                | `IsSafe`                                                                                                                   |
| `SafetymonitorInfoUpdate`     | `SafetymonitorInfoUpdate` | Safetymonitor info updated                  | [Safety Monitor Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/SafetyMonitorInfo)   |
| `SwitchConnected`             | `Equipment`               | Switch connected                            | None                                                                                                                       |
| `SwitchDisconnected`          | `Equipment`               | Switch disconnected                         | None                                                                                                                       |
| `SwitchInfoUpdate`            | `SwitchInfoUpdate`        | Switch info updated                         | [Switch Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/SwitchInfo)                  |
| `WeatherConnected`            | `Equipment`               | Weather connected                           | None                                                                                                                       |
| `WeatherDisconnected`         | `Equipment`               | Weather disconnected                        | None                                                                                                                       |
| `WeatherInfoUpdate`           | `WeatherInfoUpdate`       | Weather info updated                        | [Weather Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/WeatherInfo)                |
| `ProcessStarted`              | `Process`                 | Process started                             | `ProcessId`                                                                                                                |
| `ProcessFinished`             | `Process`                 | Process finished                            | `ProcessId`                                                                                                                |
| `ImageSaved`                  | `Image`                   | Image saved                                 | [Image Response](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/ImageResponse)            |
| `ImagePrepared`               | `Image`                   | Image prepared                              | None                                                                                                                       |
| `ProfileAdded`                | `Profile`                 | Profile added                               | [Profile Meta](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/ProfileMeta)                |
| `ProfileChanged`              | `Profile`                 | Profile changed                             | [Profile Meta](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/ProfileMeta)                |
| `ProfileRemoved`              | `Profile`                 | Profile removed                             | [Profile Meta](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/ProfileMeta)                |
| `SequenceEntityFailed`        | `Sequence`                | Sequence entity failed                      | `Entity`, `Error`                                                                                                          |
| `SequenceCustomEvent`         | `Sequence`                | Sequence custom event                       | A string containing the message as defined in the instruction                                                              |
| `SequenceStarted`             | `Sequence`                | Sequence started                            | None                                                                                                                       |
| `SequenceFinished`            | `Sequence`                | Sequence finished                           | None                                                                                                                       |
| `LivestackStatusUpdate`       | `Livestack`               | Livestack status update                     | `Status`                                                                                                                   |
| `LivestackStackUpdated`       | `Livestack`               | Livestack stack updated                     | [Livestack Image Info](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api#model/LivestackImageInfo) |
| `TppaAlignmentError`          | `TPPA`                    | Tppa alignment error                        | TODO                                                                                                                       |
| `TppaProgressUpdate`          | `TPPA`                    | Tppa progress update                        | `Status`, `Progress`                                                                                                       |
| `TsWaitStarted`               | `TargetScheduler`         | Ts wait started                             | [See Headers](https://tcpalmer.github.io/nina-scheduler/adv-topics/pub-sub.html#starting-a-wait)                           |
| `TsNewtargetStarted`          | `TargetScheduler`         | Ts new target started                       | [See Headers](https://tcpalmer.github.io/nina-scheduler/adv-topics/pub-sub.html#starting-a-new-target)                     |
| `TsTargetStarted`             | `TargetScheduler`         | Ts target started                           | [See Headers](https://tcpalmer.github.io/nina-scheduler/adv-topics/pub-sub.html#starting-a-target)                         |
| `TsContainerStopped`          | `TargetScheduler`         | Ts container stopped                        | [See Headers](https://tcpalmer.github.io/nina-scheduler/adv-topics/pub-sub.html#container-stopped)                         |
| `TsTargetComplete`            | `TargetScheduler`         | Ts target complete                          | [See Headers](https://tcpalmer.github.io/nina-scheduler/adv-topics/pub-sub.html#completing-a-target)                       |

### Channels

There are in total 21 different channels:

- `Autofocus` (subscribed by default)
- `Equipment` (subscribed by default)
- `Guiding` (subscribed by default)
- `Image` (subscribed by default)
- `Livestack` (subscribed by default)
- `Process` (subscribed by default)
- `Profile` (subscribed by default)
- `Sequence` (subscribed by default)
- `TargetScheduler` (subscribed by default)
- `TPPA` (subscribed by default)
- `CameraInfoUpdate`
- `DomeInfoUpdate`
- `FilterwheelInfoUpdate`
- `FlatdeviceInfoUpdate`
- `FocuserInfoUpdate`
- `GuiderInfoUpdate`
- `MountInfoUpdate`
- `RotatorInfoUpdate`
- `SafetyInfoUpdate`
- `SwitchInfoUpdate`
- `WeatherInfoUpdate`
