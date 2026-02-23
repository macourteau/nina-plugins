using NINA.Plugin;
using NINA.Plugin.Interfaces;
using System.ComponentModel.Composition;

namespace NINASwitchCommands {
    [Export(typeof(IPluginManifest))]
    public class SwitchCommandsPlugin : PluginBase {
    }
}
