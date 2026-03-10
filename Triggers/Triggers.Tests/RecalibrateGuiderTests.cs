using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;

namespace Triggers.Tests;

[TestFixture]
public class RecalibrateGuiderTests {
    private Mock<IGuiderMediator> mockGuider = null!;
    private Mock<ITelescopeMediator> mockTelescope = null!;
    private Mock<IProfileService> mockProfile = null!;
    private Mock<IImagingMediator> mockImaging = null!;
    private Mock<IFilterWheelMediator> mockFilterWheel = null!;
    private Mock<IDomeMediator> mockDome = null!;
    private Mock<IDomeFollower> mockDomeFollower = null!;

    [SetUp]
    public void SetUp() {
        mockGuider = new Mock<IGuiderMediator>();
        mockTelescope = new Mock<ITelescopeMediator>();
        mockProfile = new Mock<IProfileService>();
        mockImaging = new Mock<IImagingMediator>();
        mockFilterWheel = new Mock<IFilterWheelMediator>();
        mockDome = new Mock<IDomeMediator>();
        mockDomeFollower = new Mock<IDomeFollower>();

        mockGuider.Setup(g => g.GetInfo()).Returns(new GuiderInfo { Connected = true, PixelScale = 1.0 });
        mockTelescope.Setup(t => t.GetInfo()).Returns(new TelescopeInfo { Connected = true, SiderealTime = 12.0 });
    }

    private RecalibrateGuider CreateSut() {
        return new RecalibrateGuider(
            mockGuider.Object, mockTelescope.Object, mockProfile.Object,
            mockImaging.Object, mockFilterWheel.Object,
            mockDome.Object, mockDomeFollower.Object);
    }

    private static Mock<ISequenceItem> CreateLightExposureItem() {
        var item = new Mock<ISequenceItem>();
        var exposure = item.As<IExposureItem>();
        exposure.SetupGet(e => e.ImageType).Returns("LIGHT");
        return item;
    }

    private static Mock<ISequenceItem> CreateDarkExposureItem() {
        var item = new Mock<ISequenceItem>();
        var exposure = item.As<IExposureItem>();
        exposure.SetupGet(e => e.ImageType).Returns("DARK");
        return item;
    }

    private static Mock<ISequenceItem> CreateNonExposureItem() {
        return new Mock<ISequenceItem>();
    }

    /// <summary>
    /// Populates the monitor with data spanning longer than the configured window,
    /// with high RMS values that exceed a typical threshold.
    /// Uses a very short window so tests don't need real delays.
    /// </summary>
    private void PopulateMonitorAboveThreshold(RecalibrateGuider sut, double rmsValue = 5.0) {
        // Set a tiny window so we can exceed it immediately in tests
        sut.WindowMinutes = 0.0000001;
        sut.monitor.WindowMinutes = 0.0000001;

        for (int i = 0; i < 20; i++) {
            var step = new Mock<NINA.Core.Interfaces.IGuideStep>();
            step.SetupGet(s => s.RADistanceRaw).Returns(i % 2 == 0 ? rmsValue : -rmsValue);
            step.SetupGet(s => s.DECDistanceRaw).Returns(i % 2 == 0 ? rmsValue : -rmsValue);
            sut.monitor.OnGuideEvent(null!, step.Object);
        }
    }

    private void PopulateMonitorBelowThreshold(RecalibrateGuider sut) {
        sut.WindowMinutes = 0.0000001;
        sut.monitor.WindowMinutes = 0.0000001;

        for (int i = 0; i < 20; i++) {
            var step = new Mock<NINA.Core.Interfaces.IGuideStep>();
            step.SetupGet(s => s.RADistanceRaw).Returns(i % 2 == 0 ? 0.1 : -0.1);
            step.SetupGet(s => s.DECDistanceRaw).Returns(i % 2 == 0 ? 0.1 : -0.1);
            sut.monitor.OnGuideEvent(null!, step.Object);
        }
    }

    // --- ShouldTrigger tests ---

    [Test]
    public void ShouldTrigger_WhenRmsBelowThreshold_ReturnsFalse() {
        var sut = CreateSut();
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorBelowThreshold(sut);

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_WhenRmsAboveThreshold_ReturnsTrue() {
        var sut = CreateSut();
        sut.RmsThresholdArcsec = 3.0;
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeTrue();
    }

    [Test]
    public void ShouldTrigger_DuringCooldown_ReturnsFalse() {
        var sut = CreateSut();
        sut.CooldownMinutes = 30.0;
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        // Simulate a recent calibration
        sut.lastCalibrationTime = DateTime.UtcNow;

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_AfterCooldownExpired_ReturnsTrue() {
        var sut = CreateSut();
        sut.CooldownMinutes = 0.0001; // ~6ms cooldown
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        sut.lastCalibrationTime = DateTime.UtcNow.AddMinutes(-1);

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeTrue();
    }

    [Test]
    public void ShouldTrigger_WhenGuiderNotConnected_ReturnsFalse() {
        mockGuider.Setup(g => g.GetInfo()).Returns(new GuiderInfo { Connected = false });

        var sut = CreateSut();
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_ForNonLightExposure_ReturnsFalse() {
        var sut = CreateSut();
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        var nextItem = CreateDarkExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_ForNonExposureItem_ReturnsFalse() {
        var sut = CreateSut();
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        var nextItem = CreateNonExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_WhenWindowNotFull_ReturnsFalse() {
        var sut = CreateSut();
        sut.WindowMinutes = 5.0; // 5 minute window
        sut.monitor.WindowMinutes = 5.0;
        sut.monitor.Start(mockGuider.Object, 1.0);

        // Add points with high RMS, but all at roughly the same instant
        // so the buffer span is far less than 5 minutes
        for (int i = 0; i < 50; i++) {
            var step = new Mock<NINA.Core.Interfaces.IGuideStep>();
            step.SetupGet(s => s.RADistanceRaw).Returns(i % 2 == 0 ? 10.0 : -10.0);
            step.SetupGet(s => s.DECDistanceRaw).Returns(i % 2 == 0 ? 10.0 : -10.0);
            sut.monitor.OnGuideEvent(null!, step.Object);
        }

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_WithRAAxis_UsesRAValue() {
        var sut = CreateSut();
        sut.RmsAxis = RmsAxisOption.RA;
        sut.RmsThresholdArcsec = 3.0;
        sut.WindowMinutes = 0.0000001;
        sut.monitor.WindowMinutes = 0.0000001;
        sut.monitor.Start(mockGuider.Object, 1.0);

        // Add points with high RA but low Dec
        for (int i = 0; i < 20; i++) {
            var step = new Mock<NINA.Core.Interfaces.IGuideStep>();
            step.SetupGet(s => s.RADistanceRaw).Returns(i % 2 == 0 ? 5.0 : -5.0);
            step.SetupGet(s => s.DECDistanceRaw).Returns(0.01);
            sut.monitor.OnGuideEvent(null!, step.Object);
        }

        var nextItem = CreateLightExposureItem();
        sut.ShouldTrigger(null!, nextItem.Object).Should().BeTrue();
    }

    // --- Execute tests ---

    [Test]
    public async Task Execute_InPlace_ShouldStopClearAndStartGuiding() {
        var sut = CreateSut();
        sut.SlewToOptimal = false;
        sut.monitor.Start(mockGuider.Object, 1.0);

        var callOrder = new List<string>();
        mockGuider.Setup(g => g.StopGuiding(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("StopGuiding"))
            .ReturnsAsync(true);
        mockGuider.Setup(g => g.ClearCalibration(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("ClearCalibration"))
            .ReturnsAsync(true);
        mockGuider.Setup(g => g.StartGuiding(true, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("StartGuiding"))
            .ReturnsAsync(true);

        await sut.Execute(null!, new Progress<ApplicationStatus>(), CancellationToken.None);

        callOrder.Should().ContainInOrder("StopGuiding", "ClearCalibration", "StartGuiding");
        sut.lastCalibrationTime.Should().NotBeNull();
    }

    [Test]
    public async Task Execute_InPlace_ShouldClearMonitor() {
        var sut = CreateSut();
        sut.SlewToOptimal = false;
        sut.monitor.Start(mockGuider.Object, 1.0);
        PopulateMonitorAboveThreshold(sut);

        mockGuider.Setup(g => g.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockGuider.Setup(g => g.ClearCalibration(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockGuider.Setup(g => g.StartGuiding(true, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await sut.Execute(null!, new Progress<ApplicationStatus>(), CancellationToken.None);

        sut.monitor.DataPoints.Should().Be(0);
    }

    [Test]
    public async Task Execute_WithSlew_ShouldPerformFullSequence() {
        var sut = CreateSut();
        sut.SlewToOptimal = true;
        sut.PlateSolveRecenter = false;
        sut.monitor.Start(mockGuider.Object, 1.0);

        var savedCoords = new Coordinates(10.0, 45.0, Epoch.J2000, Coordinates.RAType.Hours);
        mockTelescope.Setup(t => t.GetCurrentPosition()).Returns(savedCoords);

        var callOrder = new List<string>();
        mockGuider.Setup(g => g.StopGuiding(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("StopGuiding"))
            .ReturnsAsync(true);
        mockTelescope.Setup(t => t.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .Callback<Coordinates, CancellationToken>((c, _) => callOrder.Add($"Slew({c.RA:F1})"))
            .ReturnsAsync(true);
        mockGuider.Setup(g => g.AutoSelectGuideStar(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AutoSelectGuideStar"))
            .ReturnsAsync(true);
        mockGuider.Setup(g => g.ClearCalibration(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("ClearCalibration"))
            .ReturnsAsync(true);
        mockGuider.Setup(g => g.StartGuiding(true, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("StartGuiding(force)"))
            .ReturnsAsync(true);
        mockGuider.Setup(g => g.StartGuiding(false, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("StartGuiding(noforce)"))
            .ReturnsAsync(true);

        await sut.Execute(null!, new Progress<ApplicationStatus>(), CancellationToken.None);

        callOrder.Should().ContainInOrder(
            "StopGuiding",
            "AutoSelectGuideStar",
            "ClearCalibration",
            "StartGuiding(force)",
            "StopGuiding",
            "AutoSelectGuideStar",
            "StartGuiding(noforce)");

        // Verify it slewed to calibration position then back
        callOrder.Where(c => c.StartsWith("Slew")).Should().HaveCount(2);
    }

    // --- Validate tests ---

    [Test]
    public void Validate_WhenGuiderConnected_ShouldPass() {
        var sut = CreateSut();

        sut.Validate().Should().BeTrue();
        sut.Issues.Should().BeEmpty();
    }

    [Test]
    public void Validate_WhenGuiderNotConnected_ShouldFail() {
        mockGuider.Setup(g => g.GetInfo()).Returns(new GuiderInfo { Connected = false });

        var sut = CreateSut();

        sut.Validate().Should().BeFalse();
        sut.Issues.Should().Contain(s => s.Contains("Guider", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Validate_WhenSlewToOptimalAndTelescopeNotConnected_ShouldFail() {
        mockTelescope.Setup(t => t.GetInfo()).Returns(new TelescopeInfo { Connected = false });

        var sut = CreateSut();
        sut.SlewToOptimal = true;

        sut.Validate().Should().BeFalse();
        sut.Issues.Should().Contain(s => s.Contains("Telescope", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Validate_WhenSlewToOptimalFalse_ShouldNotRequireTelescope() {
        mockTelescope.Setup(t => t.GetInfo()).Returns(new TelescopeInfo { Connected = false });

        var sut = CreateSut();
        sut.SlewToOptimal = false;

        sut.Validate().Should().BeTrue();
    }

    [Test]
    public void Validate_WhenThresholdIsZero_ShouldFail() {
        var sut = CreateSut();
        sut.RmsThresholdArcsec = 0;

        sut.Validate().Should().BeFalse();
        sut.Issues.Should().Contain(s => s.Contains("threshold", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Validate_WhenWindowMinutesIsZero_ShouldFail() {
        var sut = CreateSut();
        sut.WindowMinutes = 0;

        sut.Validate().Should().BeFalse();
        sut.Issues.Should().Contain(s => s.Contains("window", StringComparison.OrdinalIgnoreCase));
    }

    // --- Clone tests ---

    [Test]
    public void Clone_ShouldPreserveAllProperties() {
        var sut = CreateSut();
        sut.RmsThresholdArcsec = 5.0;
        sut.WindowMinutes = 10.0;
        sut.RmsAxis = RmsAxisOption.RA;
        sut.SlewToOptimal = true;
        sut.PlateSolveRecenter = false;
        sut.CooldownMinutes = 60.0;
        sut.CalibrationDec = 20.0;
        sut.MeridianOffsetDeg = 10.0;
        sut.DitherSettleSeconds = 15.0;

        var clone = (RecalibrateGuider)sut.Clone();

        clone.RmsThresholdArcsec.Should().Be(5.0);
        clone.WindowMinutes.Should().Be(10.0);
        clone.RmsAxis.Should().Be(RmsAxisOption.RA);
        clone.SlewToOptimal.Should().BeTrue();
        clone.PlateSolveRecenter.Should().BeFalse();
        clone.CooldownMinutes.Should().Be(60.0);
        clone.CalibrationDec.Should().Be(20.0);
        clone.MeridianOffsetDeg.Should().Be(10.0);
        clone.DitherSettleSeconds.Should().Be(15.0);
    }

    [Test]
    public void Clone_ShouldNotShareMutableState() {
        var sut = CreateSut();
        sut.RmsThresholdArcsec = 5.0;

        var clone = (RecalibrateGuider)sut.Clone();
        clone.RmsThresholdArcsec = 10.0;

        sut.RmsThresholdArcsec.Should().Be(5.0);
    }
}
