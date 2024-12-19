## 2.1 (not yet released, work in progress)

I **heavily** advise everyone still using V1 to start using V2 as V1 is now deprecated and will be removed with the next minor version!
V2 will probably stay for a longer time now, I made some changes behind the scenes that will make it easier to add new features without breaking older versions

- It is now possible to add the `Access-Control-Allow-Origin: *` header to make requests via javascript without proxies possible. This does pose a security vulnerability though, so make sure you only enable it when you need to. A proxy may be better suited when in production.

### V2 Changes:
- ⚠️ **Breaking** Connection events in the websocket are now seperated into connected and disconnected ⚠️
- ⚠️ **Breaking** Changed the structure of a profile response ⚠️
- Added a new websocket for controlling TPPA and getting the current error in realtime
- Framing Assistant is now supported:
  - Set the coordinates in the framing assistant
  - Slew to coordiantes (optionally with center or rotate)
  - Set desired rotation
- Add focuser move, info contains AvailableFilters field, added filter changing using the names from AvailableFilters
- Add telescope homing and tracking modes
- Add camera cooling, warming and dew heater control
- Added a gain field to capture
- A lot more events in the websocket, see the documentation for a list of all events
- GuiderInfo now includes the last guide step with raw distance
