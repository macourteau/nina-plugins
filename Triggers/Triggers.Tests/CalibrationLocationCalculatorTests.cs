using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;

namespace Triggers.Tests;

[TestFixture]
public class CalibrationLocationCalculatorTests {

    [Test]
    public void StandardCase_ShouldComputeCorrectRAAndDec() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // RA = 6.0 + (5.0 / 15.0) = 6.333...
        result.RA.Should().BeApproximately(6.333, 0.01);
        result.Dec.Should().Be(0.0);
    }

    [Test]
    public void NearBoundary_ShouldWrapCorrectly() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 23.5, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // RA = 23.5 + 0.333 = 23.833, which is < 24 so no wrap
        result.RA.Should().BeApproximately(23.833, 0.01);
    }

    [Test]
    public void PastMidnight_ShouldWrapTo24hRange() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 23.8, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        // RA = 23.8 + 0.333 = 24.133 → wraps to 0.133
        result.RA.Should().BeApproximately(0.133, 0.01);
    }

    [Test]
    public void CustomDec_ShouldUseProvidedValue() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 12.0, meridianOffsetDegrees: 0.0, calibrationDec: 20.0);

        result.RA.Should().BeApproximately(12.0, 0.001);
        result.Dec.Should().Be(20.0);
    }

    [Test]
    public void ZeroOffset_ShouldEqualLST() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 15.0, meridianOffsetDegrees: 0.0, calibrationDec: 0.0);

        result.RA.Should().BeApproximately(15.0, 0.001);
    }

    [Test]
    public void NegativeDec_ShouldWork() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, meridianOffsetDegrees: 5.0, calibrationDec: -30.0);

        result.Dec.Should().Be(-30.0);
    }

    [Test]
    public void Result_ShouldBeJ2000Epoch() {
        var result = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
            siderealTimeHours: 6.0, meridianOffsetDegrees: 5.0, calibrationDec: 0.0);

        result.Epoch.Should().Be(Epoch.J2000);
    }
}
