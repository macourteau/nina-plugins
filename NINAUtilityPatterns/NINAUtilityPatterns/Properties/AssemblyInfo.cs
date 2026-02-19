using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin.
[assembly: Guid("77f3211e-3502-401b-a7fc-750fefecb70f")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Utility Patterns")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Compact date/time and binning tokens for image file patterns")]

// Your name
[assembly: AssemblyCompany("Marc-Antoine Courteau")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Utility Patterns")]
[assembly: AssemblyCopyright("Copyright © 2026 Marc-Antoine Courteau")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.2017")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your plugin is hosted
[assembly: AssemblyMetadata("Repository", "https://github.com/macourteau/nina-plugins")]

// [Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "FilePatterns,DateTime,Binning,Utility")]

// [Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"Adds compact date/time and binning tokens for image file patterns:

- $$CDATE$$ - Compact date (yyyyMMdd) in local time
- $$CTIME$$ - Compact time (HHmmss) in local time
- $$CDATETIME$$ - Compact date+time (yyyyMMdd_HHmmss) in local time
- $$CDATEMINUS12$$ - Compact date shifted back 12 hours (for night sessions)
- $$CDATEUTC$$ - Compact date (yyyyMMdd) in UTC
- $$CTIMEUTC$$ - Compact time (HHmmss) in UTC
- $$CDATETIMEUTC$$ - Compact date+time (yyyyMMdd_HHmmss) in UTC
- $$BINX$$ - Horizontal binning factor
- $$BINY$$ - Vertical binning factor")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]
