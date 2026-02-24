using NINA.Core.Model;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using UtilityPatterns.Locale;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading.Tasks;

namespace UtilityPatterns {
    [Export(typeof(IPluginManifest))]
    public class UtilityPatternsPlugin : PluginBase {
        private readonly IImageSaveMediator imageSaveMediator;

        private readonly ImagePattern airmassPattern;
        private readonly ImagePattern altitudePattern;
        private readonly ImagePattern azimuthPattern;
        private readonly ImagePattern binxPattern;
        private readonly ImagePattern binyPattern;
        private readonly ImagePattern cdatePattern;
        private readonly ImagePattern cdateminus12Pattern;
        private readonly ImagePattern cdatetimePattern;
        private readonly ImagePattern cdatetimeutcPattern;
        private readonly ImagePattern cdateutcPattern;
        private readonly ImagePattern ctimePattern;
        private readonly ImagePattern ctimeutcPattern;

        // Store binning values captured in BeforeImageSaved for use in BeforeFinalizeImageSaved
        private int lastBinX = 1;
        private int lastBinY = 1;

        // Store telescope position values captured in BeforeImageSaved for use in BeforeFinalizeImageSaved
        private double lastAltitude = double.NaN;
        private double lastAzimuth = double.NaN;
        private double lastAirmass = double.NaN;

        [ImportingConstructor]
        public UtilityPatternsPlugin(IOptionsVM options, IImageSaveMediator imageSaveMediator) {
            this.imageSaveMediator = imageSaveMediator;

            // Initialize patterns with current values for preview
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var nowMinus12 = now.AddHours(-12);

            airmassPattern = new ImagePattern("$$AIRMASS$$", Loc.AirmassDescription, Loc.Category) {
                Value = "1.4"
            };
            altitudePattern = new ImagePattern("$$ALT$$", Loc.AltitudeDescription, Loc.Category) {
                Value = "45.0"
            };
            azimuthPattern = new ImagePattern("$$AZ$$", Loc.AzimuthDescription, Loc.Category) {
                Value = "180.0"
            };
            binxPattern = new ImagePattern("$$BINX$$", Loc.BinXDescription, Loc.Category) {
                Value = "1"
            };
            binyPattern = new ImagePattern("$$BINY$$", Loc.BinYDescription, Loc.Category) {
                Value = "1"
            };
            cdatePattern = new ImagePattern("$$CDATE$$", Loc.CDateDescription, Loc.Category) {
                Value = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            cdateminus12Pattern = new ImagePattern("$$CDATEMINUS12$$", Loc.CDateMinus12Description, Loc.Category) {
                Value = nowMinus12.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            cdatetimePattern = new ImagePattern("$$CDATETIME$$", Loc.CDateTimeDescription, Loc.Category) {
                Value = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };
            cdatetimeutcPattern = new ImagePattern("$$CDATETIMEUTC$$", Loc.CDateTimeUtcDescription, Loc.Category) {
                Value = utcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };
            cdateutcPattern = new ImagePattern("$$CDATEUTC$$", Loc.CDateUtcDescription, Loc.Category) {
                Value = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            };
            ctimePattern = new ImagePattern("$$CTIME$$", Loc.CTimeDescription, Loc.Category) {
                Value = now.ToString("HHmmss", CultureInfo.InvariantCulture)
            };
            ctimeutcPattern = new ImagePattern("$$CTIMEUTC$$", Loc.CTimeUtcDescription, Loc.Category) {
                Value = utcNow.ToString("HHmmss", CultureInfo.InvariantCulture)
            };

            // Register all patterns in the Options > Imaging > File Patterns area
            options.AddImagePattern(airmassPattern);
            options.AddImagePattern(altitudePattern);
            options.AddImagePattern(azimuthPattern);
            options.AddImagePattern(binxPattern);
            options.AddImagePattern(binyPattern);
            options.AddImagePattern(cdatePattern);
            options.AddImagePattern(cdateminus12Pattern);
            options.AddImagePattern(cdatetimePattern);
            options.AddImagePattern(cdatetimeutcPattern);
            options.AddImagePattern(cdateutcPattern);
            options.AddImagePattern(ctimePattern);
            options.AddImagePattern(ctimeutcPattern);

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

            e.AddImagePattern(new ImagePattern(airmassPattern.Key, airmassPattern.Description, airmassPattern.Category) {
                Value = FormatOneDecimal(lastAirmass)
            });

            e.AddImagePattern(new ImagePattern(altitudePattern.Key, altitudePattern.Description, altitudePattern.Category) {
                Value = FormatOneDecimal(lastAltitude)
            });

            e.AddImagePattern(new ImagePattern(azimuthPattern.Key, azimuthPattern.Description, azimuthPattern.Category) {
                Value = FormatOneDecimal(lastAzimuth)
            });

            e.AddImagePattern(new ImagePattern(binxPattern.Key, binxPattern.Description, binxPattern.Category) {
                Value = lastBinX.ToString(CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(binyPattern.Key, binyPattern.Description, binyPattern.Category) {
                Value = lastBinY.ToString(CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdatePattern.Key, cdatePattern.Description, cdatePattern.Category) {
                Value = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdateminus12Pattern.Key, cdateminus12Pattern.Description, cdateminus12Pattern.Category) {
                Value = nowMinus12.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdatetimePattern.Key, cdatetimePattern.Description, cdatetimePattern.Category) {
                Value = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdatetimeutcPattern.Key, cdatetimeutcPattern.Description, cdatetimeutcPattern.Category) {
                Value = utcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(cdateutcPattern.Key, cdateutcPattern.Description, cdateutcPattern.Category) {
                Value = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(ctimePattern.Key, ctimePattern.Description, ctimePattern.Category) {
                Value = now.ToString("HHmmss", CultureInfo.InvariantCulture)
            });

            e.AddImagePattern(new ImagePattern(ctimeutcPattern.Key, ctimeutcPattern.Description, ctimeutcPattern.Category) {
                Value = utcNow.ToString("HHmmss", CultureInfo.InvariantCulture)
            });

            return Task.CompletedTask;
        }

        private static string FormatOneDecimal(double value) {
            return double.IsNaN(value) ? "NA" : Math.Round(value, 1).ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
