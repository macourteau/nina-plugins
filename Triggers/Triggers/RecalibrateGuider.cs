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
                MeridianOffsetDeg = MeridianOffsetDeg
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
                rmsThresholdArcsec = value;
                RaisePropertyChanged();
            }
        }

        private double windowMinutes = 5.0;

        [JsonProperty]
        public double WindowMinutes {
            get => windowMinutes;
            set {
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
                rmsAxis = value;
                RaisePropertyChanged();
            }
        }

        private bool slewToOptimal;

        [JsonProperty]
        public bool SlewToOptimal {
            get => slewToOptimal;
            set {
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
            monitor = new GuideErrorMonitor { WindowMinutes = WindowMinutes };
            var pixelScale = guiderMediator.GetInfo().PixelScale;
            monitor.Start(guiderMediator, pixelScale);
        }

        public override void SequenceBlockTeardown() {
            monitor?.Stop();
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (!(nextItem is IExposureItem exposureItem) || exposureItem.ImageType != "LIGHT") {
                return false;
            }

            if (lastCalibrationTime.HasValue) {
                var elapsed = DateTime.UtcNow - lastCalibrationTime.Value;
                if (elapsed.TotalMinutes < CooldownMinutes) {
                    return false;
                }
            }

            var guiderInfo = guiderMediator.GetInfo();
            if (!guiderInfo.Connected) {
                return false;
            }

            if (monitor == null || monitor.BufferSpanMinutes < WindowMinutes) {
                return false;
            }

            double rms = GetCurrentRmsValue();
            CurrentRms = rms;
            RaisePropertyChanged(nameof(MonitorDataPoints));

            return rms > RmsThresholdArcsec;
        }

        private double GetCurrentRmsValue() {
            return RmsAxis switch {
                RmsAxisOption.RA => monitor.RmsRA,
                RmsAxisOption.Dec => monitor.RmsDec,
                _ => monitor.RmsTotal
            };
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (SlewToOptimal) {
                await ExecuteWithSlew(progress, token);
            } else {
                await ExecuteInPlace(progress, token);
            }

            monitor?.Clear();
            lastCalibrationTime = DateTime.UtcNow;
        }

        private async Task ExecuteInPlace(IProgress<ApplicationStatus> progress, CancellationToken token) {
            progress?.Report(new ApplicationStatus { Status = PluginLoc.StoppingGuiding });
            await guiderMediator.StopGuiding(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.ClearingCalibration });
            await guiderMediator.ClearCalibration(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.StartingCalibration });
            await guiderMediator.StartGuiding(forceCalibration: true, progress, token);
        }

        private async Task ExecuteWithSlew(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var savedPosition = telescopeMediator.GetCurrentPosition();

            progress?.Report(new ApplicationStatus { Status = PluginLoc.StoppingGuiding });
            await guiderMediator.StopGuiding(token);

            // Compute and slew to calibration target
            var telescopeInfo = telescopeMediator.GetInfo();
            var calibrationTarget = CalibrationLocationCalculator.ComputeOptimalCalibrationTarget(
                telescopeInfo.SiderealTime, MeridianOffsetDeg, CalibrationDec);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.SlewingToCalibrationPosition });
            await telescopeMediator.SlewToCoordinatesAsync(calibrationTarget, token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.SelectingGuideStar });
            await guiderMediator.AutoSelectGuideStar(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.ClearingCalibration });
            await guiderMediator.ClearCalibration(token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.StartingCalibration });
            await guiderMediator.StartGuiding(forceCalibration: true, progress, token);

            progress?.Report(new ApplicationStatus { Status = PluginLoc.StoppingGuiding });
            await guiderMediator.StopGuiding(token);

            // Return to original position
            if (PlateSolveRecenter) {
                progress?.Report(new ApplicationStatus { Status = PluginLoc.PlateSolveRecentering });
                var centerItem = new Center(
                    profileService, telescopeMediator, imagingMediator, filterWheelMediator,
                    guiderMediator, domeMediator, domeFollower,
                    new PlateSolverFactoryProxy(), new WindowServiceFactory());
                centerItem.Coordinates = new InputCoordinates(savedPosition);
                await centerItem.Execute(progress, token);
            } else {
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
