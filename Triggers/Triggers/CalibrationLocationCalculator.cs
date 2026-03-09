using NINA.Astrometry;

namespace Triggers {

    public static class CalibrationLocationCalculator {

        /// <summary>
        /// Computes an optimal calibration target position near the meridian.
        /// </summary>
        /// <param name="siderealTimeHours">Current local sidereal time in hours (0-24).</param>
        /// <param name="meridianOffsetDegrees">Degrees east of the meridian (positive = east, pre-meridian for GEM).</param>
        /// <param name="calibrationDec">Declination in degrees for the calibration position.</param>
        /// <returns>J2000 coordinates for the calibration target.</returns>
        public static Coordinates ComputeOptimalCalibrationTarget(double siderealTimeHours, double meridianOffsetDegrees, double calibrationDec) {
            double ra = siderealTimeHours + (meridianOffsetDegrees / 15.0);

            // Wrap RA to [0, 24) range
            ra %= 24.0;
            if (ra < 0) {
                ra += 24.0;
            }

            return new Coordinates(ra, calibrationDec, Epoch.J2000, Coordinates.RAType.Hours);
        }
    }
}
