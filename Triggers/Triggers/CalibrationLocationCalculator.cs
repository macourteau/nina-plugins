using System;
using NINA.Astrometry;

namespace Triggers {

    public static class CalibrationLocationCalculator {

        /// <summary>
        /// Computes an optimal calibration target position near the meridian,
        /// on the same side of the meridian as the current pointing direction.
        /// </summary>
        /// <param name="siderealTimeHours">Current local sidereal time in hours (0-24).</param>
        /// <param name="currentRaHours">Current telescope RA in hours (0-24), used to determine which side of the meridian to target.</param>
        /// <param name="meridianOffsetDegrees">Magnitude of the offset from the meridian in degrees (always treated as positive).</param>
        /// <param name="calibrationDec">Declination in degrees for the calibration position.</param>
        /// <returns>J2000 coordinates for the calibration target.</returns>
        public static Coordinates ComputeOptimalCalibrationTarget(double siderealTimeHours, double currentRaHours, double meridianOffsetDegrees, double calibrationDec) {
            // Hour angle: positive = west of meridian, negative = east of meridian
            double ha = siderealTimeHours - currentRaHours;
            // Normalize to [-12, +12)
            if (ha < -12) ha += 24;
            if (ha >= 12) ha -= 24;

            // Place the calibration target on the same side of the meridian as the current pointing.
            // Use |offset| east if currently pointing east, |offset| west if currently pointing west.
            double signedOffset = ha >= 0 ? -Math.Abs(meridianOffsetDegrees) : Math.Abs(meridianOffsetDegrees);

            double ra = siderealTimeHours + (signedOffset / 15.0);

            // Wrap RA to [0, 24) range
            ra %= 24.0;
            if (ra < 0) {
                ra += 24.0;
            }

            return new Coordinates(ra, calibrationDec, Epoch.J2000, Coordinates.RAType.Hours);
        }
    }
}
