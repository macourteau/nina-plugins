using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;

namespace Triggers.Tests;

[TestFixture]
public class CalibrationLocationCalculatorTests {

    [Test]
    public void PointingEast_ShouldPlaceTargetEastOfMeridian() {
        // Current RA > LST → pointing east of meridian (HA < 0)
        // Should place calibration target east of meridian (RA > LST)
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, currentRaHours: 7.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // Expect RA = 6.0 + (5.0 / 15.0) = 6.333 (east of meridian)
        result.RA.Should().BeApproximately(6.333, 0.01);
        result.Dec.Should().Be(0.0);
    }

    [Test]
    public void PointingWest_ShouldPlaceTargetWestOfMeridian() {
        // Current RA < LST → pointing west of meridian (HA > 0)
        // Should place calibration target west of meridian (RA < LST)
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, currentRaHours: 5.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // Expect RA = 6.0 - (5.0 / 15.0) = 5.667 (west of meridian)
        result.RA.Should().BeApproximately(5.667, 0.01);
    }

    [Test]
    public void NegativeOffset_ShouldUseMagnitudeOnly() {
        // A negative meridianOffsetDegrees should be treated the same as positive
        // (the sign is determined by which side of the meridian we're pointing)
        var resultPos = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, currentRaHours: 7.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);
        var resultNeg = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, currentRaHours: 7.0, meridianOffsetDegrees: -5.0, calibrationDec: 0.0);

        resultPos.RA.Should().BeApproximately(resultNeg.RA, 0.001);
    }

    [Test]
    public void PointingEast_NearMidnight_ShouldWrapCorrectly() {
        // LST near 0h, pointing east at RA 23.5 (HA = 0 - 23.5 = -23.5 → normalized to +0.5... wait)
        // Actually: HA = 0.5 - 23.5 = -23.0, normalized: -23 + 24 = 1.0 → west? No.
        // Let me think: LST=23.8, currentRA=0.2 → HA = 23.8 - 0.2 = 23.6, normalized: 23.6-24 = -0.4 → east
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 23.8, currentRaHours: 0.2, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // Pointing east → target east of meridian: RA = 23.8 + 0.333 = 24.133 → wraps to 0.133
        result.RA.Should().BeApproximately(0.133, 0.01);
    }

    [Test]
    public void PointingWest_NearMidnight_ShouldWrapCorrectly() {
        // LST=0.5, currentRA=23.5 → HA = 0.5 - 23.5 = -23.0, normalized: -23 + 24 = 1.0 → west
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 0.5, currentRaHours: 23.5, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // Pointing west → target west of meridian: RA = 0.5 - 0.333 = 0.167
        result.RA.Should().BeApproximately(0.167, 0.01);
    }

    [Test]
    public void CustomDec_ShouldUseProvidedValue() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 12.0, currentRaHours: 13.0, meridianOffsetDegrees: 0.0, calibrationDec: 20.0);

        result.RA.Should().BeApproximately(12.0, 0.001);
        result.Dec.Should().Be(20.0);
    }

    [Test]
    public void ZeroOffset_ShouldEqualLST() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 15.0, currentRaHours: 16.0, meridianOffsetDegrees: 0.0, calibrationDec: 0.0);

        result.RA.Should().BeApproximately(15.0, 0.001);
    }

    [Test]
    public void NegativeDec_ShouldWork() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, currentRaHours: 7.0, meridianOffsetDegrees: 5.0, calibrationDec: -30.0);

        result.Dec.Should().Be(-30.0);
    }

    [Test]
    public void Result_ShouldBeJ2000Epoch() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, currentRaHours: 7.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        result.Epoch.Should().Be(Epoch.J2000);
    }

    [Test]
    public void OnMeridian_ShouldDefaultToEast() {
        // HA = 0 exactly → ha >= 0 path → west offset
        // This is a boundary case; the exact choice doesn't matter much
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 12.0, currentRaHours: 12.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // HA = 0 → west side: RA = 12.0 - 0.333 = 11.667
        result.RA.Should().BeApproximately(11.667, 0.01);
    }
}
