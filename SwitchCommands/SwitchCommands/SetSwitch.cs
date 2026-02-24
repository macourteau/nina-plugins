using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using SwitchCommands.Locale;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchCommands {

    [ExportMetadata("Name", "Set Switch")]
    [ExportMetadata("Description", "Sets a boolean switch on or off")]
    [ExportMetadata("Icon", "ButtonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Switch")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetSwitch : SequenceItem, IValidatable {
        private readonly ISwitchMediator switchMediator;

        private static readonly double SwitchTolerance = 0.00001;
        private static readonly TimeSpan SwitchTimeout = TimeSpan.FromSeconds(30);

        [ImportingConstructor]
        public SetSwitch(ISwitchMediator switchMediator) {
            this.switchMediator = switchMediator;
            WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(CreateDummyList());
            SelectedSwitch = WritableSwitches.FirstOrDefault();
        }

        private SetSwitch(SetSwitch cloneMe) : this(cloneMe.switchMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SetSwitch(this) {
                OnOff = OnOff,
                SwitchIndex = SwitchIndex
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private bool onOff;

        [JsonProperty]
        public bool OnOff {
            get => onOff;
            set {
                onOff = value;
                RaisePropertyChanged();
            }
        }

        private short switchIndex;

        [JsonProperty]
        public short SwitchIndex {
            get => switchIndex;
            set {
                if (value > -1) {
                    switchIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IWritableSwitch selectedSwitch;

        [JsonIgnore]
        public IWritableSwitch SelectedSwitch {
            get => selectedSwitch;
            set {
                selectedSwitch = value;
                SwitchIndex = (short)(WritableSwitches?.IndexOf(selectedSwitch) ?? -1);
                RaisePropertyChanged();
            }
        }

        private ReadOnlyCollection<IWritableSwitch> writableSwitches;

        public ReadOnlyCollection<IWritableSwitch> WritableSwitches {
            get => writableSwitches;
            set {
                writableSwitches = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var info = switchMediator.GetInfo();
            if (info?.Connected != true) {
                throw new SequenceEntityFailedException(Loc.SwitchNotConnected);
            }

            // Find the target switch by matching the selected switch's Id in the live collection.
            // This is more robust than using an index, which could shift if the device configuration changes.
            IWritableSwitch targetSwitch = null;
            if (SelectedSwitch != null) {
                foreach (var ws in info.WritableSwitches) {
                    if (ws.Id == SelectedSwitch.Id) {
                        targetSwitch = ws;
                        break;
                    }
                }
            }

            if (targetSwitch == null) {
                throw new SequenceEntityFailedException(Loc.NoSwitchSelected);
            }

            targetSwitch.TargetValue = OnOff ? 1.0 : 0.0;
            targetSwitch.SetValue();

            // Wait briefly then poll once, matching SwitchVM.SetSwitchValue behavior.
            // The background device update timer handles subsequent polls — calling Poll()
            // repeatedly ourselves can interfere with some switch drivers (e.g. ASCOM).
            await Task.Delay(50, token);
            targetSwitch.Poll();

            var sw = Stopwatch.StartNew();
            while (Math.Abs(targetSwitch.Value - targetSwitch.TargetValue) > SwitchTolerance) {
                token.ThrowIfCancellationRequested();
                await Task.Delay(500, token);
                if (sw.Elapsed > SwitchTimeout) {
                    throw new SequenceEntityFailedException(
                        string.Format(Loc.SetSwitchTimeout,
                            targetSwitch.Name,
                            OnOff ? "On" : "Off",
                            targetSwitch.Value));
                }
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public bool Validate() {
            try {
                var i = new List<string>();
                var info = switchMediator.GetInfo();

                if (info?.Connected != true) {
                    if (!(WritableSwitches.FirstOrDefault() is DummyBooleanSwitch)) {
                        WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(CreateDummyList());
                    }
                    i.Add(Loc.SwitchNotConnected);
                } else {
                    if (WritableSwitches.FirstOrDefault() is DummyBooleanSwitch) {
                        var booleanSwitches = info.WritableSwitches
                            .Where(IsBooleanSwitch)
                            .ToList();
                        WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(booleanSwitches);

                        if (switchIndex >= 0 && switchIndex < WritableSwitches.Count) {
                            SelectedSwitch = WritableSwitches[switchIndex];
                        } else {
                            SelectedSwitch = null;
                        }
                    }

                    if (WritableSwitches.Count == 0) {
                        i.Add(Loc.NoBooleanSwitches);
                    }
                }

                if (switchIndex >= 0 && switchIndex < WritableSwitches.Count) {
                    if (WritableSwitches[switchIndex] != SelectedSwitch) {
                        SelectedSwitch = WritableSwitches[switchIndex];
                    }
                }

                if (SelectedSwitch == null) {
                    i.Add(Loc.NoSwitchSelected);
                }

                Issues = i;
                return Issues.Count == 0;
            } catch (Exception ex) {
                Issues = new List<string>() { "An unexpected error occurred" };
                Logger.Error(ex);
                return false;
            }
        }

        internal static bool IsBooleanSwitch(IWritableSwitch sw) {
            return sw.Minimum == 0 && sw.Maximum == 1 && sw.StepSize == 1;
        }

        private static IList<IWritableSwitch> CreateDummyList() {
            var dummySwitches = new List<IWritableSwitch>();
            for (short i = 0; i < 5; i++) {
                dummySwitches.Add(new DummyBooleanSwitch((short)(i + 1)));
            }
            return dummySwitches;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetSwitch)}, SwitchIndex: {SwitchIndex}, OnOff: {OnOff}";
        }

        internal class DummyBooleanSwitch : IWritableSwitch {
            public DummyBooleanSwitch(short id) {
                Id = id;
                Name = $"Switch {id}";
            }

            public short Id { get; }
            public string Name { get; }
            public string Description => string.Empty;
            public double Value => 0;
            public double Maximum => 1;
            public double Minimum => 0;
            public double StepSize => 1;
            public double TargetValue { get; set; }
            public bool Poll() => true;
            public void SetValue() { }
        }
    }
}
