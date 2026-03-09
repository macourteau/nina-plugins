using FluentAssertions;
using Moq;
using NINA.Core.Interfaces;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;

namespace Triggers.Tests;

[TestFixture]
public class GuideErrorMonitorTests {

    private static Mock<IGuideStep> CreateGuideStep(double ra, double dec) {
        var step = new Mock<IGuideStep>();
        step.SetupGet(s => s.RADistanceRaw).Returns(ra);
        step.SetupGet(s => s.DECDistanceRaw).Returns(dec);
        return step;
    }

    [Test]
    public void EmptyBuffer_ShouldReturnZeroForAllAxes() {
        var monitor = new GuideErrorMonitor();

        monitor.RmsRA.Should().Be(0);
        monitor.RmsDec.Should().Be(0);
        monitor.RmsTotal.Should().Be(0);
        monitor.DataPoints.Should().Be(0);
    }

    [Test]
    public void AddingDataPoints_ShouldUpdateRms() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);

        // Add some data points
        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 0.5).Object);
        monitor.OnGuideEvent(null!, CreateGuideStep(-1.0, -0.5).Object);
        monitor.OnGuideEvent(null!, CreateGuideStep(0.5, 0.25).Object);

        monitor.DataPoints.Should().Be(3);
        monitor.RmsRA.Should().BeGreaterThan(0);
        monitor.RmsDec.Should().BeGreaterThan(0);
        monitor.RmsTotal.Should().BeGreaterThan(0);
    }

    [Test]
    public void PixelScale_ShouldBeApplied() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();
        double pixelScale = 2.5;

        monitor.Start(mockMediator.Object, pixelScale);

        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 0.0).Object);

        // With scale=2.5, 1.0 pixel distance * 2.5 = 2.5 arcsec
        // RMS of a single point at 1.0 with mean at 1.0 is 0, but the raw value is 1.0
        // Actually for a single data point, RMS = sqrt(m2/n) = 0 since there's no variance
        // Let's add two points to verify scale
        monitor.OnGuideEvent(null!, CreateGuideStep(-1.0, 0.0).Object);

        // Two points at 1.0 and -1.0: RMS = 1.0 pixel * scale = 2.5 arcsec
        monitor.RmsRA.Should().BeApproximately(2.5, 0.001);
    }

    [Test]
    public void Clear_ShouldResetAllValues() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);

        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 0.5).Object);
        monitor.OnGuideEvent(null!, CreateGuideStep(-1.0, -0.5).Object);

        monitor.DataPoints.Should().BeGreaterThan(0);

        monitor.Clear();

        monitor.RmsRA.Should().Be(0);
        monitor.RmsDec.Should().Be(0);
        monitor.RmsTotal.Should().Be(0);
        monitor.DataPoints.Should().Be(0);
    }

    [Test]
    public void TimeWindowTrimming_ShouldExcludeOldEntries() {
        var monitor = new GuideErrorMonitor { WindowMinutes = 0.0001 }; // ~6ms window
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);

        monitor.OnGuideEvent(null!, CreateGuideStep(10.0, 10.0).Object);

        // Wait for the data to expire
        Thread.Sleep(50);

        // Adding a data point triggers trimming
        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 1.0).Object);

        // The old point should have been trimmed, leaving only 1 data point
        monitor.DataPoints.Should().Be(1);
    }

    [Test]
    public void ConcurrentAccess_ShouldNotCorruptState() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);

        // Run concurrent adds and reads
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++) {
            int capture = i;
            tasks.Add(Task.Run(() => {
                monitor.OnGuideEvent(null!, CreateGuideStep(capture * 0.1, capture * 0.05).Object);
            }));
            tasks.Add(Task.Run(() => {
                _ = monitor.RmsRA;
                _ = monitor.RmsDec;
                _ = monitor.RmsTotal;
                _ = monitor.DataPoints;
            }));
        }

        Task.WaitAll(tasks.ToArray());

        monitor.DataPoints.Should().Be(10);
    }

    [Test]
    public void BufferSpanMinutes_EmptyBuffer_ReturnsZero() {
        var monitor = new GuideErrorMonitor();

        monitor.BufferSpanMinutes.Should().Be(0);
    }

    [Test]
    public void BufferSpanMinutes_SinglePoint_ReturnsZero() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);
        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 1.0).Object);

        monitor.BufferSpanMinutes.Should().Be(0);
    }

    [Test]
    public void BufferSpanMinutes_MultiplePoints_ReturnsPositiveSpan() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);
        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 1.0).Object);

        // Even a tiny delay produces a measurable span
        Thread.Sleep(10);
        monitor.OnGuideEvent(null!, CreateGuideStep(2.0, 2.0).Object);

        monitor.BufferSpanMinutes.Should().BeGreaterThan(0);
    }

    [Test]
    public void BufferSpanMinutes_AfterClear_ReturnsZero() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);
        monitor.OnGuideEvent(null!, CreateGuideStep(1.0, 1.0).Object);
        Thread.Sleep(10);
        monitor.OnGuideEvent(null!, CreateGuideStep(2.0, 2.0).Object);

        monitor.Clear();

        monitor.BufferSpanMinutes.Should().Be(0);
    }

    [Test]
    public void Start_ShouldSubscribeToGuideEvent() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);

        mockMediator.VerifyAdd(m => m.GuideEvent += It.IsAny<EventHandler<IGuideStep>>(), Times.Once);
    }

    [Test]
    public void Stop_ShouldUnsubscribeFromGuideEvent() {
        var monitor = new GuideErrorMonitor();
        var mockMediator = new Mock<IGuiderMediator>();

        monitor.Start(mockMediator.Object, 1.0);
        monitor.Stop();

        mockMediator.VerifyRemove(m => m.GuideEvent -= It.IsAny<EventHandler<IGuideStep>>(), Times.Once);
    }
}
