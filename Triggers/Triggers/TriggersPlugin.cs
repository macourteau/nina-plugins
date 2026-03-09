using NINA.Plugin;
using NINA.Plugin.Interfaces;
using System.ComponentModel.Composition;

namespace Triggers {
    [Export(typeof(IPluginManifest))]
    public class TriggersPlugin : PluginBase {
    }
}
