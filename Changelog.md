The changes for each individual beta release can be seen [here](https://github.com/christian-photo/ninaAPI/releases)

## 2.1.0.* (betas) and 2.1.1.0

I **heavily** advise everyone still using V1 to start using V2 as V1 is now deprecated and will be removed with the next minor version!
V2 will probably stay for a longer time now, I made some changes behind the scenes that will make it easier to add new features without breaking older versions.

The documentation for the [api](https://bump.sh/christian-photo/doc/advanced-api) and the [websockets](https://bump.sh/christian-photo/doc/advanced-api-websockets/) will be hosted externally from now on

- It is now possible to add the `Access-Control-Allow-Origin: *` header to make requests via javascript without proxies possible. This does pose a security vulnerability though, so make sure you only enable it when you need to. A proxy may be better suited when in production.
- Eventwatchers are now started independently of the api, therefore Events can still be retrieved using event-history even if the api wasn't running before.

### V2 Changes:
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
