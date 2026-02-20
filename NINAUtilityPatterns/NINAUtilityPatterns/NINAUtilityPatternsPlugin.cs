using NINA.Core.Model;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINAUtilityPatterns.Locale;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading.Tasks;

namespace NINAUtilityPatterns {
    [Export(typeof(IPluginManifest))]
    public class NINAUtilityPatternsPlugin : PluginBase {
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

        // Binning patterns
        private readonly ImagePattern binxPattern;
        private readonly ImagePattern binyPattern;

        // Telescope position patterns
        private readonly ImagePattern altitudePattern;
        private readonly ImagePattern azimuthPattern;
        private readonly ImagePattern airmassPattern;

        // Store binning values captured in BeforeImageSaved for use in BeforeFinalizeImageSaved
        private int lastBinX = 1;
        private int lastBinY = 1;

        // Store telescope position values captured in BeforeImageSaved for use in BeforeFinalizeImageSaved
        private double lastAltitude = double.NaN;
        private double lastAzimuth = double.NaN;
        private double lastAirmass = double.NaN;

        [ImportingConstructor]
        public NINAUtilityPatternsPlugin(IOptionsVM options, IImageSaveMediator imageSaveMediator) {
            this.imageSaveMediator = imageSaveMediator;

            // Initialize patterns with current values for preview
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var nowMinus12 = now.AddHours(-12);

            cdatePattern = new ImagePattern("$$CDATE$$", Loc.CDateDescription, Loc.Category) {
                Value = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            ctimePattern = new ImagePattern("$$CTIME$$", Loc.CTimeDescription, Loc.Category) {
                Value = now.ToString("HHmmss", CultureInfo.InvariantCulture)
            };
            cdatetimePattern = new ImagePattern("$$CDATETIME$$", Loc.CDateTimeDescription, Loc.Category) {
                Value = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };
            cdateminus12Pattern = new ImagePattern("$$CDATEMINUS12$$", Loc.CDateMinus12Description, Loc.Category) {
                Value = nowMinus12.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            cdateutcPattern = new ImagePattern("$$CDATEUTC$$", Loc.CDateUtcDescription, Loc.Category) {
                Value = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            ctimeutcPattern = new ImagePattern("$$CTIMEUTC$$", Loc.CTimeUtcDescription, Loc.Category) {
                Value = utcNow.ToString("HHmmss", CultureInfo.InvariantCulture)
            };
            cdatetimeutcPattern = new ImagePattern("$$CDATETIMEUTC$$", Loc.CDateTimeUtcDescription, Loc.Category) {
                Value = utcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };
            binxPattern = new ImagePattern("$$BINX$$", Loc.BinXDescription, Loc.Category) {
                Value = "1"
            };
            binyPattern = new ImagePattern("$$BINY$$", Loc.BinYDescription, Loc.Category) {
                Value = "1"
            };
            altitudePattern = new ImagePattern("$$ALT$$", Loc.AltitudeDescription, Loc.Category) {
                Value = "45.0"
            };
            azimuthPattern = new ImagePattern("$$AZ$$", Loc.AzimuthDescription, Loc.Category) {
                Value = "180.0"
            };
            airmassPattern = new ImagePattern("$$AIRMASS$$", Loc.AirmassDescription, Loc.Category) {
                Value = "1.4"
            };

            // Register all patterns in the Options > Imaging > File Patterns area
            options.AddImagePattern(cdatePattern);
            options.AddImagePattern(ctimePattern);
            options.AddImagePattern(cdatetimePattern);
            options.AddImagePattern(cdateminus12Pattern);
            options.AddImagePattern(cdateutcPattern);
            options.AddImagePattern(ctimeutcPattern);
            options.AddImagePattern(cdatetimeutcPattern);
            options.AddImagePattern(binxPattern);
            options.AddImagePattern(binyPattern);
            options.AddImagePattern(altitudePattern);
            options.AddImagePattern(azimuthPattern);
            options.AddImagePattern(airmassPattern);

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
            lastBinY = e.Image.MetaData.Camera.BinY;

            // Capture telescope position from image metadata
            lastAltitude = e.Image.MetaData.Telescope.Altitude;
            lastAzimuth = e.Image.MetaData.Telescope.Azimuth;
            lastAirmass = e.Image.MetaData.Telescope.Airmass;
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

            // Binning
            e.AddImagePattern(new ImagePattern(binxPattern.Key, binxPattern.Description, binxPattern.Category) {
                Value = lastBinX.ToString(CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(binyPattern.Key, binyPattern.Description, binyPattern.Category) {
                Value = lastBinY.ToString(CultureInfo.InvariantCulture)
            });

            // Telescope position
            e.AddImagePattern(new ImagePattern(altitudePattern.Key, altitudePattern.Description, altitudePattern.Category) {
                Value = FormatOneDecimal(lastAltitude)
            });

            e.AddImagePattern(new ImagePattern(azimuthPattern.Key, azimuthPattern.Description, azimuthPattern.Category) {
                Value = FormatOneDecimal(lastAzimuth)
            });

            e.AddImagePattern(new ImagePattern(airmassPattern.Key, airmassPattern.Description, airmassPattern.Category) {
                Value = FormatOneDecimal(lastAirmass)
            });

            return Task.CompletedTask;
        }

        private static string FormatOneDecimal(double value) {
            return double.IsNaN(value) ? "NA" : Math.Round(value, 1).ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
