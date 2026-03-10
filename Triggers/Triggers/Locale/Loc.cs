using System.Globalization;
using System.Resources;

namespace Triggers.Locale {
    /// <summary>
    /// Provides access to localized strings for the plugin.
    /// </summary>
    public static class Loc {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("Triggers.Locale.Strings", typeof(Loc).Assembly);

        /// <summary>
        /// Gets a localized string by key, using the current UI culture.
        /// </summary>
        public static string GetString(string name) {
            return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
        }

        public static string RecalibrateGuiderName => GetString("RecalibrateGuiderName");
        public static string RecalibrateGuiderDescription => GetString("RecalibrateGuiderDescription");
        public static string GuiderNotConnected => GetString("GuiderNotConnected");
        public static string TelescopeNotConnected => GetString("TelescopeNotConnected");
        public static string InvalidRmsThreshold => GetString("InvalidRmsThreshold");
        public static string InvalidWindowMinutes => GetString("InvalidWindowMinutes");
        public static string StoppingGuiding => GetString("StoppingGuiding");
        public static string ClearingCalibration => GetString("ClearingCalibration");
        public static string StartingCalibration => GetString("StartingCalibration");
        public static string SlewingToCalibrationPosition => GetString("SlewingToCalibrationPosition");
        public static string ReturningToTarget => GetString("ReturningToTarget");
        public static string PlateSolveRecentering => GetString("PlateSolveRecentering");
        public static string SelectingGuideStar => GetString("SelectingGuideStar");
        public static string ResumingGuiding => GetString("ResumingGuiding");
        public static string LblWhen => GetString("LblWhen");
        public static string LblRmsOver => GetString("LblRmsOver");
        public static string LblMinExceeds => GetString("LblMinExceeds");
        public static string LblCurrent => GetString("LblCurrent");
        public static string LblSlewToOptimal => GetString("LblSlewToOptimal");
        public static string LblDec => GetString("LblDec");
        public static string LblMeridianOffset => GetString("LblMeridianOffset");
        public static string LblPlateSolveRecenter => GetString("LblPlateSolveRecenter");
        public static string LblCooldown => GetString("LblCooldown");
        public static string LblMin => GetString("LblMin");
        public static string LblDataPoints => GetString("LblDataPoints");
        public static string TooltipRmsAxis => GetString("TooltipRmsAxis");
        public static string TooltipWindowMinutes => GetString("TooltipWindowMinutes");
        public static string TooltipRmsThreshold => GetString("TooltipRmsThreshold");
        public static string TooltipSlewToOptimal => GetString("TooltipSlewToOptimal");
        public static string TooltipCalibrationDec => GetString("TooltipCalibrationDec");
        public static string TooltipMeridianOffset => GetString("TooltipMeridianOffset");
        public static string TooltipPlateSolveRecenter => GetString("TooltipPlateSolveRecenter");
        public static string TooltipCooldown => GetString("TooltipCooldown");
        public static string LblDitherSettle => GetString("LblDitherSettle");
        public static string LblSec => GetString("LblSec");
        public static string TooltipDitherSettle => GetString("TooltipDitherSettle");
    }
}
