using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System.Globalization;

namespace NINAUtilityPatterns.Tests;

[TestFixture]
public class NINAUtilityPatternsPluginTests {
    private Mock<IOptionsVM> mockOptions = null!;
    private Mock<IImageSaveMediator> mockMediator = null!;
    private List<ImagePattern> registeredPatterns = null!;

    [SetUp]
    public void SetUp() {
        mockOptions = new Mock<IOptionsVM>();
        mockMediator = new Mock<IImageSaveMediator>();
        registeredPatterns = new List<ImagePattern>();

        mockOptions
            .Setup(o => o.AddImagePattern(It.IsAny<ImagePattern>()))
            .Callback<ImagePattern>(p => registeredPatterns.Add(p));
    }

    [Test]
    public void Constructor_ShouldRegisterAllNinePatterns() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        registeredPatterns.Should().HaveCount(9);
    }

    [Test]
    public void Constructor_ShouldRegisterPatternsWithCorrectKeys() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var keys = registeredPatterns.Select(p => p.Key).ToList();
        keys.Should().Contain("$$CDATE$$");
        keys.Should().Contain("$$CTIME$$");
        keys.Should().Contain("$$CDATETIME$$");
        keys.Should().Contain("$$CDATEMINUS12$$");
        keys.Should().Contain("$$CDATEUTC$$");
        keys.Should().Contain("$$CTIMEUTC$$");
        keys.Should().Contain("$$CDATETIMEUTC$$");
        keys.Should().Contain("$$BINX$$");
        keys.Should().Contain("$$BINY$$");
    }

    [Test]
    public void Constructor_ShouldSetPreviewValuesInCorrectFormat() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var cdatePattern = registeredPatterns.First(p => p.Key == "$$CDATE$$");
        var ctimePattern = registeredPatterns.First(p => p.Key == "$$CTIME$$");
        var cdatetimePattern = registeredPatterns.First(p => p.Key == "$$CDATETIME$$");

        // Date should be 8 digits (yyyyMMdd)
        cdatePattern.Value.Should().MatchRegex(@"^\d{8}$");

        // Time should be 6 digits (HHmmss)
        ctimePattern.Value.Should().MatchRegex(@"^\d{6}$");

        // DateTime should be yyyyMMdd_HHmmss (15 chars with underscore)
        cdatetimePattern.Value.Should().MatchRegex(@"^\d{8}_\d{6}$");
    }

    [Test]
    public void Constructor_ShouldSetUtcPreviewValuesInCorrectFormat() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var cdateutcPattern = registeredPatterns.First(p => p.Key == "$$CDATEUTC$$");
        var ctimeutcPattern = registeredPatterns.First(p => p.Key == "$$CTIMEUTC$$");
        var cdatetimeutcPattern = registeredPatterns.First(p => p.Key == "$$CDATETIMEUTC$$");

        // UTC Date should be 8 digits (yyyyMMdd)
        cdateutcPattern.Value.Should().MatchRegex(@"^\d{8}$");

        // UTC Time should be 6 digits (HHmmss)
        ctimeutcPattern.Value.Should().MatchRegex(@"^\d{6}$");

        // UTC DateTime should be yyyyMMdd_HHmmss (15 chars with underscore)
        cdatetimeutcPattern.Value.Should().MatchRegex(@"^\d{8}_\d{6}$");
    }

    [Test]
    public void Constructor_ShouldSetDateMinus12PreviewValueInCorrectFormat() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var cdateminus12Pattern = registeredPatterns.First(p => p.Key == "$$CDATEMINUS12$$");

        // Should be 8 digits (yyyyMMdd)
        cdateminus12Pattern.Value.Should().MatchRegex(@"^\d{8}$");
    }

    [Test]
    public void Constructor_ShouldSetBinningPreviewValuesToOne() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var binxPattern = registeredPatterns.First(p => p.Key == "$$BINX$$");
        var binyPattern = registeredPatterns.First(p => p.Key == "$$BINY$$");

        binxPattern.Value.Should().Be("1");
        binyPattern.Value.Should().Be("1");
    }

    [Test]
    public void Constructor_ShouldSubscribeToImageSaveEvents() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        mockMediator.VerifyAdd(m => m.BeforeImageSaved += It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>(), Times.Once);
        mockMediator.VerifyAdd(m => m.BeforeFinalizeImageSaved += It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(), Times.Once);
    }

    [Test]
    public async Task Teardown_ShouldUnsubscribeFromEvents() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        await plugin.Teardown();

        mockMediator.VerifyRemove(m => m.BeforeImageSaved -= It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>(), Times.Once);
        mockMediator.VerifyRemove(m => m.BeforeFinalizeImageSaved -= It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(), Times.Once);
    }

    [Test]
    public void Constructor_ShouldSetAllPatternsToSameCategory() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var categories = registeredPatterns.Select(p => p.Category).Distinct().ToList();
        categories.Should().HaveCount(1);
        categories.First().Should().Be("NINA Utility Patterns");
    }

    [Test]
    public void Constructor_ShouldSetDescriptionsForAllPatterns() {
        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        foreach (var pattern in registeredPatterns) {
            pattern.Description.Should().NotBeNullOrWhiteSpace($"Pattern {pattern.Key} should have a description");
        }
    }

    [Test]
    public void ResolvePatterns_ShouldAddAllNinePatterns() {
        Func<object, BeforeFinalizeImageSavedEventArgs, Task>? resolveHandler = null;
        mockMediator
            .SetupAdd(m => m.BeforeFinalizeImageSaved += It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>())
            .Callback<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(h => resolveHandler = h);

        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        // Create real event args with mocked IRenderedImage
        var mockRenderedImage = new Mock<IRenderedImage>();
        var eventArgs = new BeforeFinalizeImageSavedEventArgs(mockRenderedImage.Object);

        resolveHandler!.Invoke(this, eventArgs);

        eventArgs.Patterns.Should().HaveCount(9);
    }

    [Test]
    public void ResolvePatterns_ShouldFormatDateAsYyyyMMdd() {
        Func<object, BeforeFinalizeImageSavedEventArgs, Task>? resolveHandler = null;
        mockMediator
            .SetupAdd(m => m.BeforeFinalizeImageSaved += It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>())
            .Callback<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(h => resolveHandler = h);

        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var mockRenderedImage = new Mock<IRenderedImage>();
        var eventArgs = new BeforeFinalizeImageSavedEventArgs(mockRenderedImage.Object);

        resolveHandler!.Invoke(this, eventArgs);

        var cdatePattern = eventArgs.Patterns.First(p => p.Key == "$$CDATE$$");
        cdatePattern.Value.Should().MatchRegex(@"^\d{8}$");

        // Verify it's a valid date
        DateTime.TryParseExact(cdatePattern.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            .Should().BeTrue();
    }

    [Test]
    public void ResolvePatterns_ShouldFormatTimeAsHHmmss() {
        Func<object, BeforeFinalizeImageSavedEventArgs, Task>? resolveHandler = null;
        mockMediator
            .SetupAdd(m => m.BeforeFinalizeImageSaved += It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>())
            .Callback<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(h => resolveHandler = h);

        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var mockRenderedImage = new Mock<IRenderedImage>();
        var eventArgs = new BeforeFinalizeImageSavedEventArgs(mockRenderedImage.Object);

        resolveHandler!.Invoke(this, eventArgs);

        var ctimePattern = eventArgs.Patterns.First(p => p.Key == "$$CTIME$$");
        ctimePattern.Value.Should().MatchRegex(@"^\d{6}$");

        // Verify it's a valid time
        DateTime.TryParseExact(ctimePattern.Value, "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            .Should().BeTrue();
    }

    [Test]
    public void ResolvePatterns_ShouldShiftDateBack12Hours() {
        Func<object, BeforeFinalizeImageSavedEventArgs, Task>? resolveHandler = null;
        mockMediator
            .SetupAdd(m => m.BeforeFinalizeImageSaved += It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>())
            .Callback<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(h => resolveHandler = h);

        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        var mockRenderedImage = new Mock<IRenderedImage>();
        var eventArgs = new BeforeFinalizeImageSavedEventArgs(mockRenderedImage.Object);

        resolveHandler!.Invoke(this, eventArgs);

        var cdatePattern = eventArgs.Patterns.First(p => p.Key == "$$CDATE$$");
        var cdateminus12Pattern = eventArgs.Patterns.First(p => p.Key == "$$CDATEMINUS12$$");

        var currentDate = DateTime.ParseExact(cdatePattern.Value, "yyyyMMdd", CultureInfo.InvariantCulture);
        var shiftedDate = DateTime.ParseExact(cdateminus12Pattern.Value, "yyyyMMdd", CultureInfo.InvariantCulture);

        // The shifted date should be at most 1 day before current date
        (currentDate - shiftedDate).Days.Should().BeInRange(0, 1);
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(2, 1)]
    [TestCase(3, 3)]
    public void ResolvePatterns_ShouldUseCapturedBinningValues(int expectedBinX, int expectedBinY) {
        Func<object, BeforeImageSavedEventArgs, Task>? captureHandler = null;
        Func<object, BeforeFinalizeImageSavedEventArgs, Task>? resolveHandler = null;

        mockMediator
            .SetupAdd(m => m.BeforeImageSaved += It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>())
            .Callback<Func<object, BeforeImageSavedEventArgs, Task>>(h => captureHandler = h);
        mockMediator
            .SetupAdd(m => m.BeforeFinalizeImageSaved += It.IsAny<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>())
            .Callback<Func<object, BeforeFinalizeImageSavedEventArgs, Task>>(h => resolveHandler = h);

        var plugin = new NINAUtilityPatternsPlugin(mockOptions.Object, mockMediator.Object);

        // Create ImageMetaData with binning values
        var metaData = new ImageMetaData {
            Camera = {
                BinX = expectedBinX,
                BinY = expectedBinY
            }
        };

        // Create mock IImageData with MetaData
        var mockImageData = new Mock<IImageData>();
        mockImageData.SetupGet(i => i.MetaData).Returns(metaData);

        // Create real BeforeImageSavedEventArgs with mocked dependencies
        var mockRenderedImage = new Mock<IRenderedImage>();
        var prepareTask = Task.FromResult(mockRenderedImage.Object);
        var captureEventArgs = new BeforeImageSavedEventArgs(mockImageData.Object, prepareTask);

        // Trigger capture
        captureHandler!.Invoke(this, captureEventArgs);

        // Now resolve patterns
        var resolveEventArgs = new BeforeFinalizeImageSavedEventArgs(mockRenderedImage.Object);
        resolveHandler!.Invoke(this, resolveEventArgs);

        var binxPattern = resolveEventArgs.Patterns.First(p => p.Key == "$$BINX$$");
        var binyPattern = resolveEventArgs.Patterns.First(p => p.Key == "$$BINY$$");

        binxPattern.Value.Should().Be(expectedBinX.ToString());
        binyPattern.Value.Should().Be(expectedBinY.ToString());
    }
}
