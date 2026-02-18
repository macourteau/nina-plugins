using NINA.Core.Model;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading.Tasks;

namespace NINAUtilityPatterns {
    [Export(typeof(IPluginManifest))]
    public class NINAUtilityPatternsPlugin : PluginBase {
        private const string Category = "NINA Utility Patterns";

        private readonly IImageSaveMediator imageSaveMediator;

        // Compact date/time patterns (local time)
        private readonly ImagePattern cdatePattern;
        private readonly ImagePattern ctimePattern;
        private readonly ImagePattern cdatetimePattern;

        // Date shifted back 12 hours (useful for night sessions)
        private readonly ImagePattern cdateminus12Pattern;

        // Compact date/time patterns (UTC)
        private readonly ImagePattern cdateutcPattern;
        private readonly ImagePattern ctimeutcPattern;
        private readonly ImagePattern cdatetimeutcPattern;

        // Compact binning
        private readonly ImagePattern cbinPattern;

        // Store binning value captured in BeforeImageSaved for use in BeforeFinalizeImageSaved
        private int lastBinX = 1;

        [ImportingConstructor]
        public NINAUtilityPatternsPlugin(IOptionsVM options, IImageSaveMediator imageSaveMediator) {
            this.imageSaveMediator = imageSaveMediator;

            // Initialize patterns with current values for preview
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var nowMinus12 = now.AddHours(-12);

            cdatePattern = new ImagePattern("$$CDATE$$", "Compact date (yyyyMMdd) in local time", Category) {
                Value = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            ctimePattern = new ImagePattern("$$CTIME$$", "Compact time (HHmmss) in local time", Category) {
                Value = now.ToString("HHmmss", CultureInfo.InvariantCulture)
            };
            cdatetimePattern = new ImagePattern("$$CDATETIME$$", "Compact date+time (yyyyMMdd_HHmmss) in local time", Category) {
                Value = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };
            cdateminus12Pattern = new ImagePattern("$$CDATEMINUS12$$", "Compact date shifted back 12 hours", Category) {
                Value = nowMinus12.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            cdateutcPattern = new ImagePattern("$$CDATEUTC$$", "Compact date (yyyyMMdd) in UTC", Category) {
                Value = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            ctimeutcPattern = new ImagePattern("$$CTIMEUTC$$", "Compact time (HHmmss) in UTC", Category) {
                Value = utcNow.ToString("HHmmss", CultureInfo.InvariantCulture)
            };
            cdatetimeutcPattern = new ImagePattern("$$CDATETIMEUTC$$", "Compact date+time (yyyyMMdd_HHmmss) in UTC", Category) {
                Value = utcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };
            cbinPattern = new ImagePattern("$$CBIN$$", "Binning factor (assumes symmetric binning)", Category) {
                Value = "1"
            };

            // Register all patterns in the Options > Imaging > File Patterns area
            options.AddImagePattern(cdatePattern);
            options.AddImagePattern(ctimePattern);
            options.AddImagePattern(cdatetimePattern);
            options.AddImagePattern(cdateminus12Pattern);
            options.AddImagePattern(cdateutcPattern);
            options.AddImagePattern(ctimeutcPattern);
            options.AddImagePattern(cdatetimeutcPattern);
            options.AddImagePattern(cbinPattern);

            // Hook into image saving events
            this.imageSaveMediator.BeforeImageSaved += CaptureMetadata;
            this.imageSaveMediator.BeforeFinalizeImageSaved += ResolvePatterns;
        }

        public override Task Teardown() {
            imageSaveMediator.BeforeImageSaved -= CaptureMetadata;
            imageSaveMediator.BeforeFinalizeImageSaved -= ResolvePatterns;
            return base.Teardown();
        }

        private Task CaptureMetadata(object sender, BeforeImageSavedEventArgs e) {
            // Capture binning from image metadata for use in pattern resolution
            lastBinX = e.Image.MetaData.Camera.BinX;
            return Task.CompletedTask;
        }

        private Task ResolvePatterns(object sender, BeforeFinalizeImageSavedEventArgs e) {
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var nowMinus12 = now.AddHours(-12);

            // Local time patterns
            e.AddImagePattern(new ImagePattern(cdatePattern.Key, cdatePattern.Description, cdatePattern.Category) {
                Value = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(ctimePattern.Key, ctimePattern.Description, ctimePattern.Category) {
                Value = now.ToString("HHmmss", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdatetimePattern.Key, cdatetimePattern.Description, cdatetimePattern.Category) {
                Value = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            });

            // Date shifted back 12 hours
            e.AddImagePattern(new ImagePattern(cdateminus12Pattern.Key, cdateminus12Pattern.Description, cdateminus12Pattern.Category) {
                Value = nowMinus12.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            });

            // UTC patterns
            e.AddImagePattern(new ImagePattern(cdateutcPattern.Key, cdateutcPattern.Description, cdateutcPattern.Category) {
                Value = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(ctimeutcPattern.Key, ctimeutcPattern.Description, ctimeutcPattern.Category) {
                Value = utcNow.ToString("HHmmss", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdatetimeutcPattern.Key, cdatetimeutcPattern.Description, cdatetimeutcPattern.Category) {
                Value = utcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            });

            // Binning - use captured BinX value (assumes symmetric binning)
            e.AddImagePattern(new ImagePattern(cbinPattern.Key, cbinPattern.Description, cbinPattern.Category) {
                Value = lastBinX.ToString(CultureInfo.InvariantCulture)
            });

            return Task.CompletedTask;
        }
    }
}
