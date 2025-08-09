using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin
[assembly: Guid("48b5bc05-f0d3-465e-a233-e2fe77d6e1a6")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new build of a plugin
[assembly: AssemblyVersion("2.2.8.0")]
[assembly: AssemblyFileVersion("2.2.8.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Advanced API")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("An experimental API for N.I.N.A.")]

// Your name
[assembly: AssemblyCompany("Christian Palm")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Advanced API")]
[assembly: AssemblyCopyright("Copyright ©  2025")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.1.2.9001")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/christian-photo/ninaAPI")]
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/christian-photo/ninaAPI/blob/main/Changelog.md")]
[assembly: AssemblyMetadata("Homepage", "https://buymeacoffee.com/christian.palm")]


// The following attributes are optional for the official manifest meta data

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "API,Web")]

//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"This plugin features an HTTP server, including an api and websockets!

## Features:
- Listen to events like connections, meridian flips and much more
- TPPA integration (Requires TPPA to be installed)
- Livestack integration (Requires Livestack to be installed)
- Supports basic control over the equipment and application in general
- And much more!

---

If you have question/issues/feedback, you can create an issue [here](https://github.com/christian-photo/ninaAPI/issues), take a look at the
[documentation](https://bump.sh/christian-photo/doc/advanced-api/) and here for the [websockets](https://bump.sh/christian-photo/doc/advanced-api-websockets) or just ask on the N.I.N.A. discord in [#plugin-discussions](https://discord.com/channels/436650817295089664/854531935660146718)
**Thanks to szymon and notzeeg (discord) for their help!**")]


// Setting ComVisible to false makes the types in this assembly not visible
// to COM components. If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]