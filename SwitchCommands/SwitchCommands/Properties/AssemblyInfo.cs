using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SwitchCommands.Tests")]

// [MANDATORY] The following GUID is used as a unique identifier of the plugin.
[assembly: Guid("d4a2e9f7-6b3c-4d8e-a5f1-2c7b9e4d6a3f")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Switch Commands")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Boolean on/off sequencer instruction for switches")]

// Your name
[assembly: AssemblyCompany("Marc-Antoine Courteau")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Switch Commands")]
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
[assembly: AssemblyMetadata("Tags", "Switch,Sequencer,Boolean")]

// [Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"Adds a ""Set Switch"" sequencer instruction for boolean switches (on/off).

The built-in ""Set Switch Value"" instruction passes values through the expression engine, which can fail for boolean switches. This plugin provides a dedicated boolean toggle that directly manipulates the switch, replicating the reliable code path used by the Equipment panel's ON/OFF toggles.

Features:
- Simple on/off checkbox control
- Dropdown shows only boolean switches (Min=0, Max=1, Step=1)
- Direct switch manipulation for reliable operation
- Proper error handling with timeout detection")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]
