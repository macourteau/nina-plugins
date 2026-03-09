using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Triggers {

    public class GuideErrorMonitor : BaseINPC {
        private readonly object syncLock = new object();
        private readonly List<(DateTime timestamp, double raDist, double decDist)> buffer = new();
        private readonly RMS rms = new RMS();
        private IGuiderMediator guiderMediator;

        private double windowMinutes = 5.0;

        public double WindowMinutes {
            get => windowMinutes;
            set {
                windowMinutes = value;
                RaisePropertyChanged();
            }
        }

        public double RmsRA {
            get {
                lock (syncLock) {
                    return rms.RA * rms.Scale;
                }
            }
        }

        public double RmsDec {
            get {
                lock (syncLock) {
                    return rms.Dec * rms.Scale;
                }
            }
        }

        public double RmsTotal {
            get {
                lock (syncLock) {
                    return rms.Total * rms.Scale;
                }
            }
        }

        public int DataPoints {
            get {
                lock (syncLock) {
                    return rms.DataPoints;
                }
            }
        }

        /// <summary>
        /// Returns the time span in minutes between the oldest and newest data points in the buffer.
        /// Returns 0 if the buffer has fewer than 2 points.
        /// </summary>
        public double BufferSpanMinutes {
            get {
                lock (syncLock) {
                    if (buffer.Count < 2) {
                        return 0;
                    }
                    return (buffer[buffer.Count - 1].timestamp - buffer[0].timestamp).TotalMinutes;
                }
            }
        }

        public void Start(IGuiderMediator guiderMediator, double pixelScale) {
            this.guiderMediator = guiderMediator;
            lock (syncLock) {
                rms.SetScale(pixelScale);
            }
            guiderMediator.GuideEvent += OnGuideEvent;
        }

        public void Stop() {
            if (guiderMediator != null) {
                guiderMediator.GuideEvent -= OnGuideEvent;
            }
        }

        public void Clear() {
            lock (syncLock) {
                buffer.Clear();
                rms.Clear();
            }
            RaisePropertyChanged(nameof(RmsRA));
            RaisePropertyChanged(nameof(RmsDec));
            RaisePropertyChanged(nameof(RmsTotal));
            RaisePropertyChanged(nameof(DataPoints));
        }

        internal void OnGuideEvent(object sender, IGuideStep step) {
            var now = DateTime.UtcNow;
            var cutoff = now.AddMinutes(-WindowMinutes);

            lock (syncLock) {
                // Add the data point
                double ra = step.RADistanceRaw;
                double dec = step.DECDistanceRaw;
                buffer.Add((now, ra, dec));
                rms.AddDataPoint(ra, dec);

                // Trim old entries
                while (buffer.Count > 0 && buffer[0].timestamp < cutoff) {
                    var old = buffer[0];
                    buffer.RemoveAt(0);
                    rms.RemoveDataPoint(old.raDist, old.decDist);
                }
            }

            RaisePropertyChanged(nameof(RmsRA));
            RaisePropertyChanged(nameof(RmsDec));
            RaisePropertyChanged(nameof(RmsTotal));
            RaisePropertyChanged(nameof(DataPoints));
        }
    }
}
