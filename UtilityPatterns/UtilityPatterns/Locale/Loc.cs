using System.Globalization;
using System.Resources;

namespace UtilityPatterns.Locale {
    /// <summary>
    /// Provides access to localized strings for the plugin.
    /// </summary>
    public static class Loc {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("UtilityPatterns.Locale.Strings", typeof(Loc).Assembly);

        /// <summary>
        /// Gets a localized string by key, using the current UI culture.
        /// </summary>
        public static string GetString(string name) {
            return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
        }

        // Category
        public static string Category => GetString("Category");

        // Pattern descriptions
        public static string CDateDescription => GetString("CDateDescription");
        public static string CTimeDescription => GetString("CTimeDescription");
        public static string CDateTimeDescription => GetString("CDateTimeDescription");
        public static string CDateMinus12Description => GetString("CDateMinus12Description");
        public static string CDateUtcDescription => GetString("CDateUtcDescription");
        public static string CTimeUtcDescription => GetString("CTimeUtcDescription");
        public static string CDateTimeUtcDescription => GetString("CDateTimeUtcDescription");
        public static string BinXDescription => GetString("BinXDescription");
        public static string BinYDescription => GetString("BinYDescription");
        public static string AltitudeDescription => GetString("AltitudeDescription");
        public static string AzimuthDescription => GetString("AzimuthDescription");
        public static string AirmassDescription => GetString("AirmassDescription");
    }
}
