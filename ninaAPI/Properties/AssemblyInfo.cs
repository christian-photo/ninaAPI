using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin
[assembly: Guid("48b5bc05-f0d3-465e-a233-e2fe77d6e1a6")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Advanced API")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Extend the N.I.N.A. API!")]

// Your name
[assembly: AssemblyCompany("Christian Palm")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Advanced API")]
[assembly: AssemblyCopyright("Copyright ©  2022")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "2.0.0.2036")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/rennmaus-coder/ninaAPI")]


// The following attributes are optional for the official manifest meta data

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "API,Web")]

//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"This plugin adds an advanced API to N.I.N.A.  
If you have question/issues/feedback, you can create an issue [here](https://github.com/rennmaus-coder/ninaAPI/issues), take a look at the
[documentation](https://github.com/rennmaus-coder/ninaAPI/wiki) or just ask on the N.I.N.A. discord in [#plugin-discussions](https://discord.com/channels/436650817295089664/854531935660146718)    
**Huge thanks to szymon and notzeeg (discord) for all the help and support you gave me to make the API Secure!**  

There is an issue, that if you close N.I.N.A. within the alt+tab mode, N.I.N.A. will 'crash' but this is only a cosmetic issue and everything will work normally afterwards.

---

## **Exposing the API with SSL**
If you want to expose the API over the internet, you should defenitely use SSL and the API key. You can use [this](https://github.com/rennmaus-coder/ninaAPI/blob/main/Create%20pfx.ps1) link to download a powershell
script to generate a self-signed SSL certificate. After you successfully generated the certificate, you have to specify the path to the certificate and the password. Then you must create an API key. You can either
generate one using the 'Generate API Key' button or you can use your own key. You have to send the key over the request header to the API { apikey = <your_key> }.")]


// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]