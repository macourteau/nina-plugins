using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Triggers.Tests")]

// [MANDATORY] The following GUID is used as a unique identifier of the plugin.
[assembly: Guid("c8e3a1b5-4d7f-9e2c-b6a0-f5d8c3e1a7b4")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Triggers")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Sequencer triggers for automated guider recalibration")]

// Your name
[assembly: AssemblyCompany("Marc-Antoine Courteau")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Triggers")]
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
[assembly: AssemblyMetadata("Tags", "Guider,Trigger,Calibration,PHD2")]

// [Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"Adds a ""Recalibrate Guider"" sequencer trigger that monitors guiding RMS error and automatically recalibrates the guider when it exceeds a configurable threshold.

Features:
- Time-windowed RMS monitoring for RA, Dec, or Total axes
- Configurable arcsecond threshold and time window
- In-place calibration or slew to optimal sky position
- Plate-solve re-centering after returning from calibration position
- Cooldown timer to prevent excessive recalibrations")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]
