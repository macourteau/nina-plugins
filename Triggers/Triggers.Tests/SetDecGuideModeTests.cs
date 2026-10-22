using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;

namespace Triggers.Tests;

[TestFixture]
public class SetDecGuideModeTests {
    private Mock<IGuiderMediator> mockGuider = null!;
    private Mock<ITelescopeMediator> mockTelescope = null!;

    /// <summary>Records modes instead of talking to PHD2.</summary>
    private class RecordingSetDecGuideMode : SetDecGuideMode {
        public List<DecGuideModeOption> Sent { get; } = new();
        public Exception? FailWith { get; set; }

        public RecordingSetDecGuideMode(IGuiderMediator guider, ITelescopeMediator telescope) : base(guider, telescope) { }

        internal override Task SendDecGuideMode(DecGuideModeOption mode) {
            if (FailWith != null) {
                throw FailWith;
            }
            Sent.Add(mode);
            return Task.CompletedTask;
        }
    }

    [SetUp]
    public void SetUp() {
        mockGuider = new Mock<IGuiderMediator>();
        mockTelescope = new Mock<ITelescopeMediator>();
        mockGuider.Setup(g => g.GetInfo()).Returns(new GuiderInfo { Connected = true });
        SetPierSide(PierSide.pierWest);
    }

    private void SetPierSide(PierSide side, bool connected = true) {
        mockTelescope.Setup(t => t.GetInfo()).Returns(new TelescopeInfo { Connected = connected, SideOfPier = side });
    }

    private RecordingSetDecGuideMode CreateSut() {
        var sut = new RecordingSetDecGuideMode(mockGuider.Object, mockTelescope.Object) {
            PierEastMode = DecGuideModeOption.North,
            PierWestMode = DecGuideModeOption.South
        };
        sut.SequenceBlockInitialize();
        return sut;
    }

    private static ISequenceItem LightExposure() {
        var item = new Mock<ISequenceItem>();
        item.As<IExposureItem>().SetupGet(e => e.ImageType).Returns("LIGHT");
        return item.Object;
    }

    private static ISequenceItem DarkExposure() {
        var item = new Mock<ISequenceItem>();
        item.As<IExposureItem>().SetupGet(e => e.ImageType).Returns("DARK");
        return item.Object;
    }

    // --- ModeFor ---

    [TestCase(PierSide.pierEast, DecGuideModeOption.North)]
    [TestCase(PierSide.pierWest, DecGuideModeOption.South)]
    [TestCase(PierSide.pierUnknown, null)]
    public void ModeFor_MapsPierSideToConfiguredMode(PierSide side, DecGuideModeOption? expected) {
        CreateSut().ModeFor(side).Should().Be(expected);
    }

    // --- ShouldTrigger ---

    [Test]
    public void ShouldTrigger_FirstLightExposure_ReturnsTrue() {
        CreateSut().ShouldTrigger(null!, LightExposure()).Should().BeTrue();
    }

    [Test]
    public void ShouldTrigger_NonLightItems_ReturnsFalse() {
        var sut = CreateSut();
        sut.ShouldTrigger(null!, DarkExposure()).Should().BeFalse();
        sut.ShouldTrigger(null!, new Mock<ISequenceItem>().Object).Should().BeFalse();
        sut.ShouldTrigger(null!, null!).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_GuiderDisconnected_ReturnsFalse() {
        mockGuider.Setup(g => g.GetInfo()).Returns(new GuiderInfo { Connected = false });
        CreateSut().ShouldTrigger(null!, LightExposure()).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_TelescopeDisconnected_ReturnsFalse() {
        SetPierSide(PierSide.pierWest, connected: false);
        CreateSut().ShouldTrigger(null!, LightExposure()).Should().BeFalse();
    }

    [Test]
    public void ShouldTrigger_PierSideUnknown_ReturnsFalse() {
        SetPierSide(PierSide.pierUnknown);
        CreateSut().ShouldTrigger(null!, LightExposure()).Should().BeFalse();
    }

    [Test]
    public async Task ShouldTrigger_ModeAlreadyAppliedForSamePierSide_ReturnsFalse() {
        var sut = CreateSut();
        await sut.Execute(null!, null!, CancellationToken.None);
        sut.ShouldTrigger(null!, LightExposure()).Should().BeFalse();
    }

    [Test]
    public async Task ShouldTrigger_AfterPierSideChange_ReturnsTrue() {
        var sut = CreateSut();
        await sut.Execute(null!, null!, CancellationToken.None);

        SetPierSide(PierSide.pierEast);
        sut.ShouldTrigger(null!, LightExposure()).Should().BeTrue();
    }

    [Test]
    public async Task ShouldTrigger_PierSideChangeWithSameMode_ReturnsFalse() {
        var sut = CreateSut();
        sut.PierEastMode = DecGuideModeOption.South;
        await sut.Execute(null!, null!, CancellationToken.None);

        SetPierSide(PierSide.pierEast);
        sut.ShouldTrigger(null!, LightExposure()).Should().BeFalse();
    }

    [Test]
    public async Task ShouldTrigger_AfterSequenceBlockInitialize_ReappliesMode() {
        var sut = CreateSut();
        await sut.Execute(null!, null!, CancellationToken.None);

        sut.SequenceBlockInitialize();
        sut.ShouldTrigger(null!, LightExposure()).Should().BeTrue();
    }

    // --- Execute ---

    [Test]
    public async Task Execute_SendsModeForCurrentPierSide() {
        var sut = CreateSut();
        await sut.Execute(null!, null!, CancellationToken.None);

        SetPierSide(PierSide.pierEast);
        await sut.Execute(null!, null!, CancellationToken.None);

        sut.Sent.Should().Equal(DecGuideModeOption.South, DecGuideModeOption.North);
        sut.AppliedMode.Should().Be(DecGuideModeOption.North);
        sut.AppliedPierSide.Should().Be(PierSide.pierEast);
    }

    [Test]
    public async Task Execute_PierSideUnknown_SendsNothing() {
        SetPierSide(PierSide.pierUnknown);
        var sut = CreateSut();
        await sut.Execute(null!, null!, CancellationToken.None);

        sut.Sent.Should().BeEmpty();
        sut.AppliedMode.Should().BeNull();
    }

    [Test]
    public async Task Execute_SendFails_RethrowsAndRetriesNextTime() {
        var sut = CreateSut();
        sut.FailWith = new InvalidOperationException("PHD2 down");

        var act = () => sut.Execute(null!, null!, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        sut.AppliedMode.Should().BeNull();
        sut.ShouldTrigger(null!, LightExposure()).Should().BeTrue();
    }

    // --- Validate ---

    [Test]
    public void Validate_GuiderNotPhd2_ReportsIssue() {
        mockGuider.Setup(g => g.GetDevice()).Returns(new Mock<IGuider>().Object);
        var sut = CreateSut();
        sut.Validate().Should().BeFalse();
        sut.Issues.Should().ContainSingle().Which.Should().Be(Locale.Loc.GuiderNotPhd2);
    }

    [Test]
    public void Validate_TelescopeDisconnected_ReportsIssue() {
        mockGuider.Setup(g => g.GetDevice()).Returns(new Mock<IGuider>().Object);
        SetPierSide(PierSide.pierWest, connected: false);
        var sut = CreateSut();
        sut.Validate().Should().BeFalse();
        sut.Issues.Should().Contain(Locale.Loc.TelescopeNotConnected);
    }

    // --- Clone ---

    [Test]
    public void Clone_CopiesModes() {
        var sut = CreateSut();
        var clone = (SetDecGuideMode)sut.Clone();
        clone.PierEastMode.Should().Be(DecGuideModeOption.North);
        clone.PierWestMode.Should().Be(DecGuideModeOption.South);
    }
}
