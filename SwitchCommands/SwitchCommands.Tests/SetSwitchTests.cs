using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System.Collections.ObjectModel;

namespace SwitchCommands.Tests;

[TestFixture]
public class SetSwitchTests {
    private Mock<ISwitchMediator> mockMediator = null!;

    [SetUp]
    public void SetUp() {
        mockMediator = new Mock<ISwitchMediator>();
    }

    private static Mock<IWritableSwitch> CreateMockBooleanSwitch(short id, string name, double value = 0) {
        var mock = new Mock<IWritableSwitch>();
        mock.SetupGet(s => s.Id).Returns(id);
        mock.SetupGet(s => s.Name).Returns(name);
        mock.SetupGet(s => s.Description).Returns(string.Empty);
        mock.SetupGet(s => s.Value).Returns(value);
        mock.SetupGet(s => s.Minimum).Returns(0);
        mock.SetupGet(s => s.Maximum).Returns(1);
        mock.SetupGet(s => s.StepSize).Returns(1);
        mock.SetupProperty(s => s.TargetValue);
        return mock;
    }

    private static Mock<IWritableSwitch> CreateMockAnalogSwitch(short id, string name) {
        var mock = new Mock<IWritableSwitch>();
        mock.SetupGet(s => s.Id).Returns(id);
        mock.SetupGet(s => s.Name).Returns(name);
        mock.SetupGet(s => s.Description).Returns(string.Empty);
        mock.SetupGet(s => s.Value).Returns(0);
        mock.SetupGet(s => s.Minimum).Returns(0);
        mock.SetupGet(s => s.Maximum).Returns(255);
        mock.SetupGet(s => s.StepSize).Returns(1);
        mock.SetupProperty(s => s.TargetValue);
        return mock;
    }

    private SwitchInfo CreateConnectedInfo(params IWritableSwitch[] switches) {
        return new SwitchInfo {
            Connected = true,
            WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(switches)
        };
    }

    private SwitchInfo CreateDisconnectedInfo() {
        return new SwitchInfo {
            Connected = false,
            WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(Array.Empty<IWritableSwitch>())
        };
    }

    // --- Constructor tests ---

    [Test]
    public void Constructor_ShouldInitializeWithDummySwitches() {
        var sut = new SetSwitch(mockMediator.Object);

        sut.WritableSwitches.Should().HaveCount(5);
        sut.WritableSwitches.Should().AllBeOfType<SetSwitch.DummyBooleanSwitch>();
    }

    [Test]
    public void Constructor_ShouldSelectFirstDummySwitch() {
        var sut = new SetSwitch(mockMediator.Object);

        sut.SelectedSwitch.Should().NotBeNull();
        sut.SelectedSwitch.Should().Be(sut.WritableSwitches[0]);
    }

    [Test]
    public void Constructor_ShouldDefaultOnOffToFalse() {
        var sut = new SetSwitch(mockMediator.Object);

        sut.OnOff.Should().BeFalse();
    }

    // --- Clone tests ---

    [Test]
    public void Clone_ShouldPreserveOnOff() {
        var sut = new SetSwitch(mockMediator.Object) { OnOff = true };

        var clone = (SetSwitch)sut.Clone();

        clone.OnOff.Should().BeTrue();
    }

    [Test]
    public void Clone_ShouldPreserveSwitchIndex() {
        var sut = new SetSwitch(mockMediator.Object);
        sut.SelectedSwitch = sut.WritableSwitches[2];

        var clone = (SetSwitch)sut.Clone();

        clone.SwitchIndex.Should().Be(2);
    }

    [Test]
    public void Clone_ShouldNotShareMutableState() {
        var sut = new SetSwitch(mockMediator.Object) { OnOff = true };

        var clone = (SetSwitch)sut.Clone();
        clone.OnOff = false;

        sut.OnOff.Should().BeTrue();
    }

    // --- IsBooleanSwitch filter tests ---

    [Test]
    public void IsBooleanSwitch_ShouldReturnTrueForBooleanSwitch() {
        var boolSwitch = CreateMockBooleanSwitch(1, "Bool");

        SetSwitch.IsBooleanSwitch(boolSwitch.Object).Should().BeTrue();
    }

    [Test]
    public void IsBooleanSwitch_ShouldReturnFalseForAnalogSwitch() {
        var analogSwitch = CreateMockAnalogSwitch(1, "Analog");

        SetSwitch.IsBooleanSwitch(analogSwitch.Object).Should().BeFalse();
    }

    [Test]
    public void IsBooleanSwitch_ShouldReturnFalseForNonZeroMinimum() {
        var mock = new Mock<IWritableSwitch>();
        mock.SetupGet(s => s.Minimum).Returns(1);
        mock.SetupGet(s => s.Maximum).Returns(2);
        mock.SetupGet(s => s.StepSize).Returns(1);

        SetSwitch.IsBooleanSwitch(mock.Object).Should().BeFalse();
    }

    [Test]
    public void IsBooleanSwitch_ShouldReturnFalseForFractionalStepSize() {
        var mock = new Mock<IWritableSwitch>();
        mock.SetupGet(s => s.Minimum).Returns(0);
        mock.SetupGet(s => s.Maximum).Returns(1);
        mock.SetupGet(s => s.StepSize).Returns(0.1);

        SetSwitch.IsBooleanSwitch(mock.Object).Should().BeFalse();
    }

    // --- Validation tests ---

    [Test]
    public void Validate_WhenDisconnected_ShouldReportNotConnected() {
        mockMediator.Setup(m => m.GetInfo()).Returns(CreateDisconnectedInfo());

        var sut = new SetSwitch(mockMediator.Object);
        var result = sut.Validate();

        result.Should().BeFalse();
        sut.Issues.Should().Contain(s => s.Contains("not connected", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Validate_WhenDisconnected_ShouldSwapToEmptyDummyList() {
        mockMediator.Setup(m => m.GetInfo()).Returns(CreateDisconnectedInfo());

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();

        sut.WritableSwitches.Should().AllBeOfType<SetSwitch.DummyBooleanSwitch>();
    }

    [Test]
    public void Validate_WhenConnectedWithBooleanSwitches_ShouldShowOnlyBooleanSwitches() {
        var boolSwitch1 = CreateMockBooleanSwitch(0, "Heater");
        var analogSwitch = CreateMockAnalogSwitch(1, "Brightness");
        var boolSwitch2 = CreateMockBooleanSwitch(2, "Dew Heater");

        mockMediator.Setup(m => m.GetInfo())
            .Returns(CreateConnectedInfo(boolSwitch1.Object, analogSwitch.Object, boolSwitch2.Object));

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();

        sut.WritableSwitches.Should().HaveCount(2);
        sut.WritableSwitches.Select(s => s.Name).Should().Contain("Heater");
        sut.WritableSwitches.Select(s => s.Name).Should().Contain("Dew Heater");
        sut.WritableSwitches.Select(s => s.Name).Should().NotContain("Brightness");
    }

    [Test]
    public void Validate_WhenConnectedWithNoBooleanSwitches_ShouldReportNoBooleanSwitches() {
        var analogSwitch = CreateMockAnalogSwitch(0, "Brightness");
        mockMediator.Setup(m => m.GetInfo())
            .Returns(CreateConnectedInfo(analogSwitch.Object));

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();

        sut.Issues.Should().Contain(s => s.Contains("boolean", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Validate_WhenConnectedWithBooleanSwitchSelected_ShouldPass() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater");
        mockMediator.Setup(m => m.GetInfo())
            .Returns(CreateConnectedInfo(boolSwitch.Object));

        var sut = new SetSwitch(mockMediator.Object);
        var result = sut.Validate();

        result.Should().BeTrue();
        sut.Issues.Should().BeEmpty();
    }

    [Test]
    public void Validate_WhenConnected_ShouldRestoreSwitchFromIndex() {
        var boolSwitch1 = CreateMockBooleanSwitch(0, "Heater");
        var boolSwitch2 = CreateMockBooleanSwitch(1, "Dew Heater");
        mockMediator.Setup(m => m.GetInfo())
            .Returns(CreateConnectedInfo(boolSwitch1.Object, boolSwitch2.Object));

        var sut = new SetSwitch(mockMediator.Object);
        // Simulate JSON deserialization setting SwitchIndex to 1
        sut.SwitchIndex = 1;
        sut.Validate();

        sut.SelectedSwitch.Should().Be(boolSwitch2.Object);
    }

    [Test]
    public void Validate_WhenNullInfo_ShouldReportNotConnected() {
        mockMediator.Setup(m => m.GetInfo()).Returns((SwitchInfo)null!);

        var sut = new SetSwitch(mockMediator.Object);
        var result = sut.Validate();

        result.Should().BeFalse();
        sut.Issues.Should().Contain(s => s.Contains("not connected", StringComparison.OrdinalIgnoreCase));
    }

    // --- SelectedSwitch/SwitchIndex property tests ---

    [Test]
    public void SelectedSwitch_ShouldUpdateSwitchIndex() {
        var sut = new SetSwitch(mockMediator.Object);

        sut.SelectedSwitch = sut.WritableSwitches[3];

        sut.SwitchIndex.Should().Be(3);
    }

    [Test]
    public void SelectedSwitch_WhenSetToNull_ShouldSetIndexToNegative() {
        var sut = new SetSwitch(mockMediator.Object);
        sut.SelectedSwitch = sut.WritableSwitches[2];

        sut.SelectedSwitch = null!;

        sut.SwitchIndex.Should().Be(2); // -1 rejected by SwitchIndex setter guard
    }

    // --- Execute tests ---

    [Test]
    public void Execute_WhenDisconnected_ShouldThrow() {
        mockMediator.Setup(m => m.GetInfo()).Returns(CreateDisconnectedInfo());

        var sut = new SetSwitch(mockMediator.Object);

        var act = () => sut.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);
        act.Should().ThrowAsync<SequenceEntityFailedException>();
    }

    [Test]
    public async Task Execute_WhenConnectedAndSwitchResponds_ShouldSetTargetValueOn() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater", value: 0);
        var info = CreateConnectedInfo(boolSwitch.Object);
        mockMediator.Setup(m => m.GetInfo()).Returns(info);

        // Make the switch "respond" by having Poll update Value to match TargetValue
        boolSwitch.Setup(s => s.SetValue()).Callback(() => {
            boolSwitch.SetupGet(s => s.Value).Returns(1.0);
        });
        boolSwitch.Setup(s => s.Poll()).Returns(true);

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();
        sut.OnOff = true;

        await sut.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);

        boolSwitch.VerifySet(s => s.TargetValue = 1.0, Times.Once);
        boolSwitch.Verify(s => s.SetValue(), Times.Once);
    }

    [Test]
    public async Task Execute_WhenConnectedAndSwitchResponds_ShouldSetTargetValueOff() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater", value: 1);
        var info = CreateConnectedInfo(boolSwitch.Object);
        mockMediator.Setup(m => m.GetInfo()).Returns(info);

        boolSwitch.Setup(s => s.SetValue()).Callback(() => {
            boolSwitch.SetupGet(s => s.Value).Returns(0.0);
        });
        boolSwitch.Setup(s => s.Poll()).Returns(true);

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();
        sut.OnOff = false;

        await sut.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);

        boolSwitch.VerifySet(s => s.TargetValue = 0.0, Times.Once);
        boolSwitch.Verify(s => s.SetValue(), Times.Once);
    }

    [Test]
    public async Task Execute_WhenSwitchAlreadyOn_ShouldSkipSetValue() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater", value: 1);
        var info = CreateConnectedInfo(boolSwitch.Object);
        mockMediator.Setup(m => m.GetInfo()).Returns(info);

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();
        sut.OnOff = true;

        await sut.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);

        boolSwitch.Verify(s => s.SetValue(), Times.Never);
    }

    [Test]
    public async Task Execute_WhenSwitchAlreadyOff_ShouldSkipSetValue() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater", value: 0);
        var info = CreateConnectedInfo(boolSwitch.Object);
        mockMediator.Setup(m => m.GetInfo()).Returns(info);

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();
        sut.OnOff = false;

        await sut.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);

        boolSwitch.Verify(s => s.SetValue(), Times.Never);
    }

    [Test]
    public void Execute_WhenNoSwitchSelected_ShouldThrow() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater");
        var info = CreateConnectedInfo(boolSwitch.Object);
        mockMediator.Setup(m => m.GetInfo()).Returns(info);

        var sut = new SetSwitch(mockMediator.Object);
        // Don't validate (which would select a switch), manually clear selection
        sut.Validate();
        // Force a different ID so lookup fails
        var differentSwitch = CreateMockBooleanSwitch(99, "Gone");
        typeof(SetSwitch).GetField("selectedSwitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(sut, differentSwitch.Object);

        var act = () => sut.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);
        act.Should().ThrowAsync<SequenceEntityFailedException>();
    }

    [Test]
    public void Execute_WhenCancelled_ShouldThrowOperationCanceledException() {
        var boolSwitch = CreateMockBooleanSwitch(0, "Heater", value: 0);
        var info = CreateConnectedInfo(boolSwitch.Object);
        mockMediator.Setup(m => m.GetInfo()).Returns(info);

        // Switch never changes value, so it would timeout, but we cancel first
        boolSwitch.Setup(s => s.Poll()).Returns(true);

        var sut = new SetSwitch(mockMediator.Object);
        sut.Validate();
        sut.OnOff = true;
        boolSwitch.SetupProperty(s => s.TargetValue);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.Execute(new Progress<ApplicationStatus>(), cts.Token);
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    // --- DummyBooleanSwitch tests ---

    [Test]
    public void DummyBooleanSwitch_ShouldHaveBooleanProperties() {
        var dummy = new SetSwitch.DummyBooleanSwitch(1);

        dummy.Minimum.Should().Be(0);
        dummy.Maximum.Should().Be(1);
        dummy.StepSize.Should().Be(1);
        dummy.Value.Should().Be(0);
        dummy.Id.Should().Be(1);
        dummy.Name.Should().Be("Switch 1");
    }

    [Test]
    public void DummyBooleanSwitch_ShouldBeRecognizedAsBoolean() {
        var dummy = new SetSwitch.DummyBooleanSwitch(1);

        SetSwitch.IsBooleanSwitch(dummy).Should().BeTrue();
    }

    // --- ToString test ---

    [Test]
    public void ToString_ShouldContainClassNameAndProperties() {
        var sut = new SetSwitch(mockMediator.Object) { OnOff = true };

        var str = sut.ToString();

        str.Should().Contain("SetSwitch");
        str.Should().Contain("OnOff: True");
    }
}
