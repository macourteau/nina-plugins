using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyGuider.PHD2;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using PluginLoc = Triggers.Locale.Loc;

namespace Triggers {

    // Names match PHD2's dec guide mode strings exactly; they are sent verbatim.
    public enum DecGuideModeOption { Auto, North, South, Off }

    [ExportMetadata("Name", "Set Dec Guide Mode")]
    [ExportMetadata("Description", "Sets the PHD2 Dec guide mode according to the mount's side of pier")]
    [ExportMetadata("Icon", "GuiderSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Guider")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetDecGuideMode : SequenceTrigger, IValidatable {
        private const int PhdResponseTimeoutMs = 10000;

        private readonly IGuiderMediator guiderMediator;
        private readonly ITelescopeMediator telescopeMediator;

        [ImportingConstructor]
        public SetDecGuideMode(IGuiderMediator guiderMediator, ITelescopeMediator telescopeMediator) : base() {
            this.guiderMediator = guiderMediator;
            this.telescopeMediator = telescopeMediator;
        }

        private SetDecGuideMode(SetDecGuideMode cloneMe) : this(cloneMe.guiderMediator, cloneMe.telescopeMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SetDecGuideMode(this) {
                PierEastMode = PierEastMode,
                PierWestMode = PierWestMode
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

        private DecGuideModeOption pierEastMode = DecGuideModeOption.Auto;

        [JsonProperty]
        public DecGuideModeOption PierEastMode {
            get => pierEastMode;
            set {
                if (pierEastMode != value) {
                    Logger.Info($"SetDecGuideMode PierEastMode changed from {pierEastMode} to {value}");
                }
                pierEastMode = value;
                RaisePropertyChanged();
            }
        }

        private DecGuideModeOption pierWestMode = DecGuideModeOption.Auto;

        [JsonProperty]
        public DecGuideModeOption PierWestMode {
            get => pierWestMode;
            set {
                if (pierWestMode != value) {
                    Logger.Info($"SetDecGuideMode PierWestMode changed from {pierWestMode} to {value}");
                }
                pierWestMode = value;
                RaisePropertyChanged();
            }
        }

        // The mode most recently sent to PHD2 by this trigger; null until the first successful send,
        // so the mode is always (re)applied once at the start of each sequence block.
        private DecGuideModeOption? appliedMode;

        public DecGuideModeOption? AppliedMode {
            get => appliedMode;
            private set {
                appliedMode = value;
                RaisePropertyChanged();
            }
        }

        private PierSide appliedPierSide = PierSide.pierUnknown;

        public PierSide AppliedPierSide {
            get => appliedPierSide;
            private set {
                appliedPierSide = value;
                RaisePropertyChanged();
            }
        }

        internal DecGuideModeOption? ModeFor(PierSide pierSide) {
            return pierSide switch {
                PierSide.pierEast => PierEastMode,
                PierSide.pierWest => PierWestMode,
                _ => null
            };
        }

        public override void SequenceBlockInitialize() {
            AppliedMode = null;
            AppliedPierSide = PierSide.pierUnknown;
            Logger.Info($"SetDecGuideMode initialized - pier East: {PierEastMode}, pier West: {PierWestMode}");
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem is not IExposureItem exposureItem || exposureItem.ImageType != "LIGHT") {
                return false;
            }

            if (!guiderMediator.GetInfo().Connected) {
                Logger.Debug("SetDecGuideMode: guider not connected");
                return false;
            }

            var telescopeInfo = telescopeMediator.GetInfo();
            if (!telescopeInfo.Connected) {
                Logger.Debug("SetDecGuideMode: telescope not connected");
                return false;
            }

            var desired = ModeFor(telescopeInfo.SideOfPier);
            if (desired == null) {
                Logger.Debug($"SetDecGuideMode: side of pier is {telescopeInfo.SideOfPier}, leaving Dec guide mode unchanged");
                return false;
            }

            if (desired == AppliedMode) {
                Logger.Debug($"SetDecGuideMode: Dec guide mode {desired} already applied for {telescopeInfo.SideOfPier}");
                return false;
            }

            Logger.Info($"SetDecGuideMode triggered - side of pier {telescopeInfo.SideOfPier}, Dec guide mode {AppliedMode?.ToString() ?? "unknown"} -> {desired}");
            return true;
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Re-read the side of pier rather than trusting ShouldTrigger's snapshot, in case it changed in between.
            var pierSide = telescopeMediator.GetInfo().SideOfPier;
            var mode = ModeFor(pierSide);
            if (mode == null) {
                Logger.Warning($"SetDecGuideMode: side of pier became {pierSide} before execution, leaving Dec guide mode unchanged");
                return;
            }

            progress?.Report(new ApplicationStatus { Status = string.Format(PluginLoc.SettingDecGuideMode, mode) });
            try {
                await SendDecGuideMode(mode.Value);
            } catch (Exception ex) {
                Logger.Error($"SetDecGuideMode failed to set Dec guide mode {mode} for side of pier {pierSide}: {ex.Message}");
                throw;
            }

            AppliedMode = mode;
            AppliedPierSide = pierSide;
            Logger.Info($"SetDecGuideMode set PHD2 Dec guide mode to {mode} for side of pier {pierSide}");
        }

        internal virtual async Task SendDecGuideMode(DecGuideModeOption mode) {
            if (guiderMediator.GetDevice() is not PHD2Guider phd2) {
                throw new InvalidOperationException(PluginLoc.GuiderNotPhd2);
            }

            var msg = new Phd2SetDecGuideMode { Parameters = new[] { mode.ToString() } };
            var response = await phd2.SendMessage(msg, PhdResponseTimeoutMs);
            if (response == null) {
                throw new InvalidOperationException($"PHD2 did not respond to set_dec_guide_mode {mode}");
            }
            if (response.error != null) {
                throw new InvalidOperationException($"PHD2 rejected set_dec_guide_mode {mode}: {response.error.message} (code {response.error.code})");
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public bool Validate() {
            var i = new List<string>();

            if (!guiderMediator.GetInfo().Connected) {
                i.Add(PluginLoc.GuiderNotConnected);
            } else if (guiderMediator.GetDevice() is not PHD2Guider) {
                i.Add(PluginLoc.GuiderNotPhd2);
            }

            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(PluginLoc.TelescopeNotConnected);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Trigger: {nameof(SetDecGuideMode)}, PierEast: {PierEastMode}, PierWest: {PierWestMode}";
        }
    }
}
