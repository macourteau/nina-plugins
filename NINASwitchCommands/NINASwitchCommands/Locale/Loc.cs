using System.Globalization;
using System.Resources;

namespace NINASwitchCommands.Locale {
    /// <summary>
    /// Provides access to localized strings for the plugin.
    /// </summary>
    public static class Loc {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("NINASwitchCommands.Locale.Strings", typeof(Loc).Assembly);

        /// <summary>
        /// Gets a localized string by key, using the current UI culture.
        /// </summary>
        public static string GetString(string name) {
            return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
        }

        public static string SetSwitchName => GetString("SetSwitchName");
        public static string SetSwitchDescription => GetString("SetSwitchDescription");
        public static string SwitchNotConnected => GetString("SwitchNotConnected");
        public static string NoBooleanSwitches => GetString("NoBooleanSwitches");
        public static string NoSwitchSelected => GetString("NoSwitchSelected");
        public static string SetSwitchTimeout => GetString("SetSwitchTimeout");
    }
}
