The changes for each individual beta release can be seen [here](https://github.com/christian-photo/ninaAPI/releases)

## 2.1.7.0

### Changes

- ⚠️ **Breaking** Enums are now serialized as strings, check the documentation for concrete updates. Please note, that not everything might be updated correctly ⚠️
- Added a 1s delay to the `IMAGE-SAVE` event to give NINA a bit more time to finish up the image
- `image/{index}` now automatically retries the image retrieval 10 times with a 200ms delay between each try, if it fails to load the image (because it is in use for example)
- Added `waitForResult` to `framing/slew`
- Added `move-axis` to `mount` to manually move the mount axis
- Fixed solving a capture image
- Fixed an issue where the slew failed if no image was loaded in the framing assistant. An image is now automatically loaded

## 2.1.6.0

### Changes

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

### Changes

- `mount/flip` added to perform a meridian flip, the flip will only be executed if it is needed
- `mount/slew` slews the mount to the specified ra and dec angles
- `dome/set-follow` to start or stop the dome following the mount
- `dome/sync` to start a sync of mount and scope
- `dome/slew` to slew the dome to the specified azimuth angle (degree)
- `DOME-SLEWED` and `DOME-SYNCED` added as new events in the websocket

## 2.1.4.0

### Changes

- `guider/start` now accepts the parameter calibrate to force a calibration (true / false)
- Guider info now contains a State field indicating what the guider is currently doing
- `guider/clear-calibration` added to clear the current calibration
- `guider/graph` to get the last n guide steps as configured on the guide graph in NINA (in NINA you can set x to be 50, 100, 200 or 400)

## 2.1.3.0

### Changes

- Added query parameter `skipRescan` to all connect endpoints, which can be used to skip the rescanning process resulting in a faster connection
- Some websocket events now include more information:
  - `FILTERWHEEL-CHANGED` includes the previous and new filter
  - `FLAT-BRIGHTNESS-CHANGED` includes the previous and new brightness
  - `SAFETY-CHANGED` includes the new status
- Added `factor`, `blackClipping`, `unlinked` parameters to `image/{index}` to configure the stretch parameters

## 2.1.2.0

**⚠️ THIS UPDATE REMOVES V1 SUPPORT ⚠️**

### Changes

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
