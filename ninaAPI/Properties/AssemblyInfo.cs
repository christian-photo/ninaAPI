using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin
[assembly: Guid("48b5bc05-f0d3-465e-a233-e2fe77d6e1a6")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new build of a plugin
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Advanced API")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Web API for N.I.N.A.")]

// Your name
[assembly: AssemblyCompany("Christian Palm")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Advanced API")]
[assembly: AssemblyCopyright("Copyright ©  2026")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.9001")]

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
[assembly: AssemblyMetadata("LongDescription", @"# Advanced API for N.I.N.A.

A powerful HTTP and WebSocket server plugin that enables remote control and monitoring of N.I.N.A.

## Key Features

### Equipment Control
* Complete equipment connection management
* Device specific controls like camera cooling or mount slewing (these are only a very small fraction of the available controls)

### Real-time Monitoring
* Equipment status and state changes
* Sequence progress and events
* Meridian flip notifications
* Connection status updates

### Integration Support
* TPPA (Third Party Program Automation) integration
* LiveStack integration for real-time stacking

### Developer Features
* RESTful API with OpenAPI/Swagger documentation
* WebSocket support for real-time events
* Comprehensive error handling

## Documentation & Support
* [Full API Documentation](https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api)
* [WebSocket Documentation](https://github.com/christian-photo/ninaAPI/wiki/Websocket-V3)
* [Issue Tracker](https://github.com/christian-photo/ninaAPI/issues)

If you need help, want to report a bug or request a new feature, please join the N.I.N.A. discord or write an issue on the github repository!")]


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
