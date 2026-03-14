using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using PluginLoc = Triggers.Locale.Loc;

namespace Triggers {

    public enum RmsAxisOption { Total, RA, Dec }

    [ExportMetadata("Name", "Recalibrate Guider")]
    [ExportMetadata("Description", "Recalibrates the guider when RMS error exceeds a threshold")]
    [ExportMetadata("Icon", "GuiderSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Guider")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class RecalibrateGuider : SequenceTrigger, IValidatable {
        private readonly IGuiderMediator guiderMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IProfileService profileService;
        private readonly IImagingMediator imagingMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;

        internal GuideErrorMonitor monitor;
        internal DateTime? lastCalibrationTime;

        [ImportingConstructor]
        public RecalibrateGuider(
            IGuiderMediator guiderMediator,
            ITelescopeMediator telescopeMediator,
            IProfileService profileService,
            IImagingMediator imagingMediator,
            IFilterWheelMediator filterWheelMediator,
            IDomeMediator domeMediator,
            IDomeFollower domeFollower) : base() {
            this.guiderMediator = guiderMediator;
            this.telescopeMediator = telescopeMediator;
            this.profileService = profileService;
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            monitor = new GuideErrorMonitor();
        }

        private RecalibrateGuider(RecalibrateGuider cloneMe) : this(
            cloneMe.guiderMediator,
            cloneMe.telescopeMediator,
            cloneMe.profileService,
            cloneMe.imagingMediator,
            cloneMe.filterWheelMediator,
            cloneMe.domeMediator,
            cloneMe.domeFollower) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new RecalibrateGuider(this) {
                RmsThresholdArcsec = RmsThresholdArcsec,
                WindowMinutes = WindowMinutes,
                RmsAxis = RmsAxis,
                SlewToOptimal = SlewToOptimal,
                PlateSolveRecenter = PlateSolveRecenter,
                CooldownMinutes = CooldownMinutes,
                CalibrationDec = CalibrationDec,
                MeridianOffsetDeg = MeridianOffsetDeg,
                DitherSettleSeconds = DitherSettleSeconds
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        private double rmsThresholdArcsec = 3.0;

        [JsonProperty]
        public double RmsThresholdArcsec {
            get => rmsThresholdArcsec;
            set {
                if (rmsThresholdArcsec != value) {
                    Logger.Info($"RecalibrateGuider RmsThresholdArcsec changed from {rmsThresholdArcsec:F2} to {value:F2}");
                }
                rmsThresholdArcsec = value;
                RaisePropertyChanged();
            }
        }

        private double windowMinutes = 5.0;

        [JsonProperty]
        public double WindowMinutes {
            get => windowMinutes;
            set {
                if (windowMinutes != value) {
                    Logger.Info($"RecalibrateGuider WindowMinutes changed from {windowMinutes:F1} to {value:F1}");
                }
                windowMinutes = value;
                if (monitor != null) {
                    monitor.WindowMinutes = value;
                }
                RaisePropertyChanged();
            }
        }

        private RmsAxisOption rmsAxis = RmsAxisOption.Total;

        [JsonProperty]
        public RmsAxisOption RmsAxis {
            get => rmsAxis;
            set {
                if (rmsAxis != value) {
                    Logger.Info($"RecalibrateGuider RmsAxis changed from {rmsAxis} to {value}");
                }
                rmsAxis = value;
                RaisePropertyChanged();
                CurrentRms = GetCurrentRmsValue();
            }
        }

        private bool slewToOptimal;

        [JsonProperty]
        public bool SlewToOptimal {
            get => slewToOptimal;
            set {
                if (slewToOptimal != value) {
                    Logger.Info($"RecalibrateGuider SlewToOptimal changed from {slewToOptimal} to {value}");
                }
                slewToOptimal = value;
                RaisePropertyChanged();
            }
        }

        private bool plateSolveRecenter = true;

        [JsonProperty]
        public bool PlateSolveRecenter {
            get => plateSolveRecenter;
            set {
                plateSolveRecenter = value;
                RaisePropertyChanged();
            }
        }

        private double cooldownMinutes = 30.0;

        [JsonProperty]
        public double CooldownMinutes {
            get => cooldownMinutes;
            set {
                if (cooldownMinutes != value) {
                    Logger.Info($"RecalibrateGuider CooldownMinutes changed from {cooldownMinutes:F1} to {value:F1}");
                }
                cooldownMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double calibrationDec;

        [JsonProperty]
        public double CalibrationDec {
            get => calibrationDec;
            set {
                calibrationDec = value;
                RaisePropertyChanged();
            }
        }

        private double meridianOffsetDeg = 5.0;

        [JsonProperty]
        public double MeridianOffsetDeg {
            get => meridianOffsetDeg;
            set {
                meridianOffsetDeg = value;
                RaisePropertyChanged();
            }
        }

        private double ditherSettleSeconds = 10.0;

        [JsonProperty]
        public double DitherSettleSeconds {
            get => ditherSettleSeconds;
            set {
                if (ditherSettleSeconds != value) {
                    Logger.Info($"RecalibrateGuider DitherSettleSeconds changed from {ditherSettleSeconds:F1} to {value:F1}");
                }
                ditherSettleSeconds = value;
                if (monitor != null) {
                    monitor.DitherSettleSeconds = value;
                }
                RaisePropertyChanged();
            }
        }

        private double currentRms;

        public double CurrentRms {
            get => currentRms;
            set {
                currentRms = value;
                RaisePropertyChanged();
            }
        }

        public int MonitorDataPoints => monitor?.DataPoints ?? 0;

        public override void SequenceBlockInitialize() {
            monitor = new GuideErrorMonitor { WindowMinutes = WindowMinutes, DitherSettleSeconds = DitherSettleSeconds };
            monitor.PropertyChanged += OnMonitorPropertyChanged;
            var pixelScale = guiderMediator.GetInfo().PixelScale;
            monitor.Start(guiderMediator, pixelScale);
            Logger.Info($"RecalibrateGuider initialized - axis: {RmsAxis}, threshold: {RmsThresholdArcsec} arcsec, window: {WindowMinutes} min, cooldown: {CooldownMinutes} min, dither settle: {DitherSettleSeconds} s, pixel scale: {pixelScale} arcsec/px");
        }

        public override void SequenceBlockTeardown() {
            Logger.Info("RecalibrateGuider shutting down");
            if (monitor != null) {
                monitor.PropertyChanged -= OnMonitorPropertyChanged;
                monitor.Stop();
            }
        }

        private void OnMonitorPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(GuideErrorMonitor.RmsTotal)
                || e.PropertyName == nameof(GuideErrorMonitor.RmsRA)
                || e.PropertyName == nameof(GuideErrorMonitor.RmsDec)) {
                CurrentRms = GetCurrentRmsValue();
            }
            if (e.PropertyName == nameof(GuideErrorMonitor.DataPoints)) {
                RaisePropertyChanged(nameof(MonitorDataPoints));
            }
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem is not IExposureItem exposureItem || exposureItem.ImageType != "LIGHT") {
                Logger.Debug($"RecalibrateGuider: skipping {nextItem?.GetType().Name ?? "null"} (not a LIGHT exposure)");
                return false;
            }

            if (lastCalibrationTime.HasValue) {
                var elapsed = DateTime.UtcNow - lastCalibrationTime.Value;
                if (elapsed.TotalMinutes < CooldownMinutes) {
                    Logger.Debug($"RecalibrateGuider cooldown active - {elapsed.TotalMinutes:F1} of {CooldownMinutes:F1} min elapsed");
                    return false;
                }
            }

            var guiderInfo = guiderMediator.GetInfo();
            if (!guiderInfo.Connected) {
                Logger.Debug($"RecalibrateGuider: guider not connected");
                return false;
            }

            if (monitor == null) {
                Logger.Debug($"RecalibrateGuider: no monitor");
            } else if (monitor.BufferSpanMinutes < WindowMinutes) {
                Logger.Debug($"RecalibrateGuider: have {monitor.BufferSpanMinutes:F1} minutes, need at least {WindowMinutes:F1} minutes");
                return false;
            }

            double rms = GetCurrentRmsValue();
            CurrentRms = rms;
            RaisePropertyChanged(nameof(MonitorDataPoints));

            if (rms > RmsThresholdArcsec) {
                Logger.Warning($"RecalibrateGuider triggered - {RmsAxis} RMS {rms:F2} arcsec exceeds threshold {RmsThresholdArcsec:F2} arcsec ({monitor.DataPoints} data points over {monitor.BufferSpanMinutes:F1} min)");
                return true;
            }

            Logger.Debug($"RecalibrateGuider: {RmsAxis} RMS {rms:F2} arcsec below threshold {RmsThresholdArcsec:F2} arcsec");
            return false;
        }

        private double GetCurrentRmsValue() {
            return RmsAxis switch {
                RmsAxisOption.RA => monitor.RmsRA,
                RmsAxisOption.Dec => monitor.RmsDec,
                _ => monitor.RmsTotal
            };
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                if (SlewToOptimal) {
                    await ExecuteWithSlew(progress, token);
                } else {
                    await ExecuteInPlace(progress, token);
                }
            } finally {
                monitor?.Clear();
                lastCalibrationTime = DateTime.UtcNow;
            }
        }

        private async Task ExecuteInPlace(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("RecalibrateGuider executing in-place calibration");

            progress?.Report(new ApplicationStatus { Status = PluginLoc.StoppingGuiding });
            await guiderMediator.StopGuiding(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.SelectingGuideStar });
            await guiderMediator.AutoSelectGuideStar(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.ClearingCalibration });
            await guiderMediator.ClearCalibration(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.StartingCalibration });
            await guiderMediator.StartGuiding(forceCalibration: true, progress, token);

            Logger.Info("RecalibrateGuider in-place calibration complete");
        }

        private async Task ExecuteWithSlew(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var savedPosition = telescopeMediator.GetCurrentPosition();
            Logger.Info($"RecalibrateGuider executing with slew - saved position RA: {savedPosition.RA:F4}h, Dec: {savedPosition.Dec:F4}°");

            bool slewedAway = false;

            try {
                progress?.Report(new ApplicationStatus { Status = PluginLoc.StoppingGuiding });
                await guiderMediator.StopGuiding(token);

                // Compute and slew to calibration target (on the same side of the meridian as the current pointing)
                var telescopeInfo = telescopeMediator.GetInfo();
                var calibrationTarget = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
                    telescopeInfo.SiderealTime, savedPosition.RA, MeridianOffsetDeg, CalibrationDec);
                Logger.Info($"RecalibrateGuider slewing to calibration position RA: {calibrationTarget.RA:F4}h, Dec: {calibrationTarget.Dec:F4}° (LST: {telescopeInfo.SiderealTime:F4}h, current RA: {savedPosition.RA:F4}h, meridian offset: {MeridianOffsetDeg}°)");

                progress?.Report(new ApplicationStatus { Status = PluginLoc.SlewingToCalibrationPosition });
                await telescopeMediator.SlewToCoordinatesAsync(calibrationTarget, token);
                slewedAway = true;

                progress?.Report(new ApplicationStatus { Status = PluginLoc.SelectingGuideStar });
                await guiderMediator.AutoSelectGuideStar(token);

                progress?.Report(new ApplicationStatus { Status = PluginLoc.ClearingCalibration });
                await guiderMediator.ClearCalibration(token);

                progress?.Report(new ApplicationStatus { Status = PluginLoc.StartingCalibration });
                await guiderMediator.StartGuiding(forceCalibration: true, progress, token);

                progress?.Report(new ApplicationStatus { Status = PluginLoc.StoppingGuiding });
                await guiderMediator.StopGuiding(token);

                // Return to original position
                await ReturnToTarget(savedPosition, progress, token);

                Logger.Info("RecalibrateGuider slew calibration complete, guiding resumed");
            } catch (Exception ex) {
                if (slewedAway) {
                    Logger.Error($"RecalibrateGuider failed during calibration, attempting to return to target: {ex.Message}");
                    try {
                        progress?.Report(new ApplicationStatus { Status = PluginLoc.ReturningToTarget });
                        await telescopeMediator.SlewToCoordinatesAsync(savedPosition, CancellationToken.None);
                        Logger.Info("RecalibrateGuider returned to target after error");
                    } catch (Exception returnEx) {
                        Logger.Error($"RecalibrateGuider failed to return to target: {returnEx.Message}");
                    }
                }
                throw;
            }
        }

        private async Task ReturnToTarget(Coordinates savedPosition, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (PlateSolveRecenter) {
                Logger.Info("RecalibrateGuider returning to target with plate-solve centering");
                progress?.Report(new ApplicationStatus { Status = PluginLoc.PlateSolveRecentering });
                var centerItem = new Center(
                    profileService, telescopeMediator, imagingMediator, filterWheelMediator,
                    guiderMediator, domeMediator, domeFollower,
                    new PlateSolverFactoryProxy(), new WindowServiceFactory());
                centerItem.Coordinates = new InputCoordinates(savedPosition);
                await centerItem.Execute(progress, token);
            } else {
                Logger.Info("RecalibrateGuider returning to target with blind slew");
                progress?.Report(new ApplicationStatus { Status = PluginLoc.ReturningToTarget });
                await telescopeMediator.SlewToCoordinatesAsync(savedPosition, token);
            }

            progress?.Report(new ApplicationStatus { Status = PluginLoc.SelectingGuideStar });
            await guiderMediator.AutoSelectGuideStar(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.ResumingGuiding });
            await guiderMediator.StartGuiding(forceCalibration: false, progress, token);
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public bool Validate() {
            var i = new List<string>();

            var guiderInfo = guiderMediator.GetInfo();
            if (!guiderInfo.Connected) {
                i.Add(PluginLoc.GuiderNotConnected);
            }

            if (SlewToOptimal) {
                var telescopeInfo = telescopeMediator.GetInfo();
                if (!telescopeInfo.Connected) {
                    i.Add(PluginLoc.TelescopeNotConnected);
                }
            }

            if (RmsThresholdArcsec <= 0) {
                i.Add(PluginLoc.InvalidRmsThreshold);
            }

            if (WindowMinutes <= 0) {
                i.Add(PluginLoc.InvalidWindowMinutes);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Trigger: {nameof(RecalibrateGuider)}, RmsThreshold: {RmsThresholdArcsec}\", Axis: {RmsAxis}, Window: {WindowMinutes}min";
        }
    }
}
