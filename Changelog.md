# Changelog

The changes for each individual (beta) release can be seen in each [release](https://github.com/christian-photo/ninaAPI/releases). This changelog will only cover the fully released versions.

## 2.2.12.1

- When no prepared image is available, the endpoint now returns a 404 error
- The autofocus endpont `equipment/focuser/auto-focus` now correctly opens the autofocus window
- Fixed an issue where the `profile/change-value` endpoint would not work with indexed properties

## 2.2.12.0

- Minimum application version was increased to NINA 3.2!
- Added the field `TrackingMode` to the `mount/info` endpoint
- Fixes an issue where the `image-history` endpoint would return the first, now the last image when all parameters were omitted
- Added `AUTOFOCUS-STARTING` and `AUTOFOCUS-POINT-ADDED` events to the websocket
- Added `plugin/settings` to get the plugin settings
- New `livestack/status` endpoint and `STACK-STATUS` event in the websocket (thanks @rbarbera)

## 2.2.11.1

- Fixed a bug where NINA would crash if another plugin also subscribed to the Meridian Flip events

## 2.2.11.0

- Added a networked manual rotator (just like the already existing networked filter wheel)
- Added the url parameter `onlyAwaitCaptureCompletion` and `onlySaveRaw` to the `camera/capture` endpoint for faster capture. This can be used to only await the capture completion and not the image preparation. The other capture endpoints will not work when using `onlySaveRaw`.
- Added the url parameter `targetName` to the `camera/capture` endpoint to specify the name of the target that is being captured.
- Added `HFRStDev`, `Min`, `Max` and `TargetName` to the `IMAGE-SAVE` event (and `image-history`)
- Extended the STACK-UPDATED event with the following fields:
  - `IsMonochrome`
  - `StackCount` (If IsMonochrome is true)
  - `RedStackCount` (If IsMonochrome is false)
  - `GreenStackCount` (If IsMonochrome is false)
  - `BlueStackCount` (If IsMonochrome is false)
- Added `livestack/image/{target}/{filter}/info` to get information about the current stacked image

## 2.2.10.0

- Updated TPPA Socket to support progress updates (TPPA >= 2.2.4.1)
- Added `CompletedIterations` to `Smart Exposure` instruction for `sequence/json`
- `AtTargetTemp` will now correctly check if `Temperature` is equal to `TemperatureSetPoint`
- Added Content-Length header to the response

## 2.2.9.0

- Added `prepared-image` endpoint to get the current image in the image dockable.
- Added `IMAGE-PREPARED` event to the websocket. This event is sent when a new image is shown in the image dockable.

## 2.2.8.0

- Sequence endpoints don't require a DSO Container to be present anymore
- `sequence/list-available` now includes sequences and targets in subfolders
- Removed the `ADV-SEQ-START` and `ADV-SEQ-STOP` events from the websocket. Not considered a breaking change, because they were deprecated in version 2.2.1.0. Use `SEQUENCE-STARTING` and `SEQUENCE-FINISHED` instead.
- Revert fix that NaN values are now returned as 0

## 2.2.7.0

- **Important: Please use streaming instead of the base64 encoding for images, since base64 support will be removed in the near future**
- Fixes an issue with `guider/stop`
- Fixed a bug in the Livestack endpoint that could return a wrong image
- Improved memory consumption of the capture endpoint
- NaN values are now returned as 0
- Added `livestack/image/available` to get a list of available images

## 2.2.6.2

- Made the port optionally profile dependent
- Seperated the cached thumbnails by NINA instance

## 2.2.6.0

- CORS updates to support all HTTP verbs
- Added `api/time` to get the current time, can be used to sync with other systems
- Added `api/application-start` to get the application start date
- Added `api/version/nina` to get the version of NINA
- Added `api/application/logs` to get the last N log entries
- Added `sequence/load` to load sequences into the advanced sequence
  - Use GET to load a sequence from the default sequence folder (you can use the `sequence/list-available` endpoint to get the available sequences)
  - Use POST to load a sequence from a json string, supplied by the client in the request body

## 2.2.5.0

- Added center + center and rotate to `mount/slew`, both can be stopped using `mount/slew/stop`
- Use Access Control Header defaults to true now

## 2.2.4.0

- Added TPPA configuration
- Fixed incorrect urls in plugin options

## 2.2.3.0

- Added `profile/horizon` to get the horizon for the active profile
- Added `mount/sync`
- Added the `raw_fits` parameter to `image/{index}` to get the raw fits image, thanks to [#31](https://github.com/christian-photo/ninaAPI/pull/31), @vitopigno

## 2.2.2.0

- Added `ROTATOR-MOVED` and `ROTATOR-MOVED-MECHANICAL` events to the websocket
- Added `application/get-tab` to get the current application tab

## 2.2.1.0

### Sequence

- The events `ADV-SEQ-START` and `ADV-SEQ-STOP` are now **deprecated**, use `SEQUENCE-STARTING` and `SEQUENCE-FINISHED` instead. These work for both advanced and simple sequences. `ADV-SEQ-START` and `ADV-SEQ-STOP` will be removed in the future.
- Added a sequence instruction Send WebSocket Event, which sends a text message as an event to the WebSocket
- Added a sequence trigger Send Error to WebSocket, which sends an error message to the websocket if a sequence item failed

### Target Scheduler

Added multiple target scheduler events to the websocket ([TS-Docs](https://tcpalmer.github.io/nina-scheduler/adv-topics/pub-sub.html)):

- `TS-WAITSTART` to get notified when a wait starts
- `TS-NEWTARGETSTART` to get notified when a new target starts
- `TS-TARGETSTART` to get notified when a target starts

### Dome

- `dome/slew` can be stopped too using `dome/stop`
- Added `IsFollowing` and `IsSynchronized` to `dome/info`
- Added `waitToFinish` to `dome/slew` to wait until the slew is finished
- Added `dome/set-park-position` to set the park position
- Added `dome/park` to park the dome
- Added `dome/home` to find the home position

---

- Added a custom filter wheel driver, available as a websocket, to allow for remote filter change completion
- Bugfixes for `image/{index}`
- Fixed the mime type for jpg images (`image/jpg` -> `image/jpeg`)

## 2.2.0.0

From now on, breaking changes will always increment the minor version number (the second number 2.x.0.0).
Sorry for the breaking changes, but they are worth it!

- ⚠️ **Breaking** Removed `skipRescan` from `{device}/connect`. This is now always true.
- ⚠️ **Breaking** The response formats of ``{device}/connect` and `{device}/disconnect` have changed. These now only return `Connected` or `Disconnected`.
- Added `to` to `{device}/connect` to specify which device should be connected. If omitted, the currently selected device will be used. This is the behavior as it was in the previous versions.
- Added `{device}/rescan` to rescan for new devices.
- Added `{device}/list-available` to list all available devices.

---

- ⚠️ **Breaking** The mount does not automatically stop the movement anymore when disconnecting. This was causing issues with PHD and the 2 second delay should be enough as a safety measure.
- Fixed an issue where the API would show the wrong ip address
- Added the url parameter `save` to the `camera/capture` endpoint. This will save the image to the disk. This needs to be set, when capturing the image.
- Added `sequence/state`, a new sequence endpoint for retrieving information that is much more elaborate and also supports plugins.
- Added `sequence/edit`, which works similary to `profile/set-value`. Note that this mainly supports fields that expect simple types like strings, numbers etc, and may not work for things like enums or objects (filter, time source, ...). This is an **experimental** feature and may be unreliable or uncomplete.
- Added `mount/set-park-position`, which sets the current mount position as park position.
- Added `pause-alignment` and `resume-alignment` to the TPPA WebSocket.
- Added an option to create and cache thumbnails for images. This has to be enabled if you want to use the new thumbnail endpoint (`image/thumbnail`).
- Introduced `camera/capture/statistics` which analyses the captured image and returns stats like HFR, Stars, Median, ...

## 2.1.8.0

- ⚠️ **Breaking** `image/{index}` now includes more images. Every image that is recieved as a websocket event can now be loaded using this endpoint. This is because the `imageType`
  parameter was introduced. The endpoint doesn't use NINA's image history anymore, but instead uses the websocket history. This includes more images and will result in a more consistent experience.
  The issue with NINA's image history is, that it does not necessarily include all images, like calibration frames.

### Flats

- Added the following methods for capturing flats:
  - `flats/skyflat`
  - `flats/auto-brightness`
  - `flats/auto-exposure`
  - `flats/trained-dark-flat`
  - `flats/trained-flat`

These methods do exactly the same as their sequence instruction counterparts.

- Added `flats/status` to get the status of the flat taking process.
- Added `flats/stop` to stop the flat taking process.

### Mount

- Added `waitForResult` to `mount/slew` to wait for the slew to finish.
- Added `mount/slew/stop` to abort the current slew.
- Added a `mount` websocket channel to move the mount axis manually. This automatically stops all movement when the client disconnects as a safety measure to prevent any accidents.

### Image

- Added `imageType` to `image/{index}` and `image-history` to filter the images by type.
- Added `debayer` and `bayerPattern` to `image/{index}` to debayer the image.
- Added `IsBayered` to `IMAGE-SAVE` event (and `image-history`).
- Added `autoPrepare` to `image/{index}` to leave all processing up to NINA. Using this will result in the same image as the one you see in NINA

---

- Added `PROFILE-ADDED` and `PROFILE-REMOVED` events to get notified when the collection of profiles changes, `PROFILE-CHANGED` is sent when the active profile changes.

## 2.1.7.1

- ⚠️ **Breaking** Enums are now serialized as strings, check the documentation for concrete updates. Please note, that not everything might be updated correctly ⚠️
- **Added support for the Livestack plugin (>= 1.0.0.9)**
- `livestack/start` to start the livestack
- `livestack/stop` to stop the livestack
- `livestack/image/{target}/{filter}` to get the current stacked image for a given filter and target
- `STACK-UPDATED` event in the websocket added, to notify when a new image is available

- Added `waitForResult` to `framing/slew`
- Added `version` to the documentation, it was always there just not documented
- Added `application/plugins` to get a list of installed plugins

- Added a 1s delay to the `IMAGE-SAVE` event to give NINA a bit more time to finish up the image
- `image/{index}` now automatically retries the image retrieval 10 times with a 200ms delay between each try, if it fails to load the image (because it is in use for example)

- Fixed solving a capture image
- Fixed an issue where the slew failed if no image was loaded in the framing assistant. An image is now automatically loaded

## 2.1.6.0

- `framing/determine-rotation` added to determine the rotation from the camera
- `camera/set-binning` added to set the binning of the camera, the binning mode has to be supported by the camera
- `camera/capture`:
  - URL parameter `omitImage` added to ignore the captured image, use if only the platesolve result is of interest
  - URL parameter `stream` added to stream the image, content type will be either image/jpg or image/png
  - URL parameter `waitForResult` added to wait for the capture to finish and then return the result. All parameters which you would normally use together with `getResult` will apply here as well
- Added `stream` parameter to `image/{index}` to stream the image, content type will be either image/jpg or image/png
- Added `stream` parameter to `application/screenshot` to stream the image, content type will be either image/jpg or image/png
- New Websocket Events:
  - `FOCUSER-USER-FOCUSED`
  - `AUTOFOCUS-FINISHED`
  - `API-CAPTURE-FINISHED` is sent, when `camera/capture` finishes

## 2.1.5.0

- `mount/flip` added to perform a meridian flip, the flip will only be executed if it is needed
- `mount/slew` slews the mount to the specified ra and dec angles
- `dome/set-follow` to start or stop the dome following the mount
- `dome/sync` to start a sync of mount and scope
- `dome/slew` to slew the dome to the specified azimuth angle (degree)
- `DOME-SLEWED` and `DOME-SYNCED` added as new events in the websocket

## 2.1.4.0

- `guider/start` now accepts the parameter calibrate to force a calibration (true / false)
- Guider info now contains a State field indicating what the guider is currently doing
- `guider/clear-calibration` added to clear the current calibration
- `guider/graph` to get the last n guide steps as configured on the guide graph in NINA (in NINA you can set x to be 50, 100, 200 or 400)

## 2.1.3.0

- Added query parameter `skipRescan` to all connect endpoints, which can be used to skip the rescanning process resulting in a faster connection
- Some websocket events now include more information:
  - `FILTERWHEEL-CHANGED` includes the previous and new filter
  - `FLAT-BRIGHTNESS-CHANGED` includes the previous and new brightness
  - `SAFETY-CHANGED` includes the new status
- Added `factor`, `blackClipping`, `unlinked` parameters to `image/{index}` to configure the stretch parameters

## 2.1.2.0

**⚠️ THIS UPDATE REMOVES V1 SUPPORT ⚠️**

- ⚠️ **Breaking** Removed support for the deprecated v1 api ⚠️
- Added more endpoints to `flatdevice`:
  - `flatdevice/set-cover` to open or close the cover
  - `flatdevice/set-light` to toggle the light on or off
  - `flatdevice/set-brightness` to change the brightness of the flatpanel
- Added more events regarding the flat panel:
  - `FLAT-LIGHT-TOGGLED`
  - `FLAT-COVER-OPENED`
  - `FLAT-COVER-CLOSED`
  - `FLAT-BRIGHTNESS-CHANGED`
- Added `sequence/set-target` to update the target in a target container
- The server now automatically picks the next available port to launch the api
- Implemented IMessageBroker for cross Plugin communication. Use Topic `AdvancedAPI.RequestPort` to request the port the api is running on, subscribe to `AdvancedAPI.Port` to recieve the answer. The port is directly written into Content

## 2.1.0.\* (betas) and 2.1.1.0

I **heavily** advise everyone still using V1 to start using V2 as V1 is now deprecated and will be removed with the next minor version!
V2 will probably stay for a longer time now, I made some changes behind the scenes that will make it easier to add new features without breaking older versions.

The documentation for the [api](https://bump.sh/christian-photo/doc/advanced-api) and the [websockets](https://bump.sh/christian-photo/doc/advanced-api-websockets/) will be hosted externally from now on

- It is now possible to add the `Access-Control-Allow-Origin: *` header to make requests via javascript without proxies possible. This does pose a security vulnerability though, so make sure you only enable it when you need to. A proxy may be better suited when in production.
- Eventwatchers are now started independently of the api, therefore Events can still be retrieved using event-history even if the api wasn't running before.

### V2 Changes

- ⚠️ **Breaking** Connection events in the websocket are now seperated into connected and disconnected ⚠️
- ⚠️ **Breaking** Changed the structure of a profile response ⚠️
- ⚠️ **Breaking** Removed the description field from a sequence response, added more specific fields for each item ⚠️
- ⚠️ **Breaking** `sequence/start` will now return an error if the sequence is already running ⚠️
- ⚠️ **Breaking** Image (history) now returns an error when there are no images available ⚠️
- ⚠️ **Breaking** Removed Id field from image-history response ⚠️
- Added a new websocket for controlling TPPA and getting the current error in realtime
- Framing Assistant is now supported:
  - Set the coordinates in the framing assistant
  - Slew to coordiantes (optionally with center or rotate)
  - Set desired rotation
- Add focuser move, info contains AvailableFilters field, added filter changing using the names from AvailableFilters, filter-info returns info about a given filter
- Autofocus can be cancelled (only if it was started using the api)
- Add telescope homing and tracking modes
- Add camera cooling, warming and dew heater control
- Add `TargetTemp` and `AtTargetTemp` to CameraInfo
- Added a gain field to capture
- A lot more events in the websocket, see the documentation for a list of all events
- GuiderInfo now includes the last guide step with raw distance
- The Sequence json is now more sophisticated with additional fields for most sequence items
- Added Conditions (global ones too) and triggers to the sequence json
- Added `sequence/reset` to reset the progress of a sequence
- When retrieving images, you can now specify `scale` (0.1 to 1) to scale the image down while preserving its aspect ratio
- Added the field `ImageType` to the IMAGE-SAVE Event and image-history
- All Image Types (LIGHT, DARK, BIAS, FLAT, SNAPSHOT) now raise an IMAGE-SAVE event
- `framing/set-source` added
- `camera/set-readout` added to set the readout mode
- `version` now returns the Plugin Version instead of an Independent Version number
