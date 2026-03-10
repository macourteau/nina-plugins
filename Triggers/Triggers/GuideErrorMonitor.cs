using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Triggers {

    public class GuideErrorMonitor : BaseINPC {
        private readonly object syncLock = new object();
        private readonly List<(DateTime timestamp, double raDist, double decDist)> buffer = new();
        private readonly RMS rms = new RMS();
        private IGuiderMediator guiderMediator;
        private DateTime? lastDitherTime;

        private double windowMinutes = 5.0;

        public double WindowMinutes {
            get => windowMinutes;
            set {
                windowMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double ditherSettleSeconds = 10.0;

        public double DitherSettleSeconds {
            get => ditherSettleSeconds;
            set {
                ditherSettleSeconds = value;
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
        /// Returns the time span in minutes from the oldest data point in the buffer to now.
        /// Returns 0 if the buffer is empty.
        /// </summary>
        public double BufferSpanMinutes {
            get {
                lock (syncLock) {
                    if (buffer.Count == 0) {
                        return 0;
                    }
                    return (DateTime.UtcNow - buffer[0].timestamp).TotalMinutes;
                }
            }
        }

        public void Start(IGuiderMediator guiderMediator, double pixelScale) {
            this.guiderMediator = guiderMediator;
            lock (syncLock) {
                rms.SetScale(pixelScale);
            }
            guiderMediator.GuideEvent += OnGuideEvent;
            guiderMediator.AfterDither += OnAfterDither;
            Logger.Debug($"GuideErrorMonitor started - pixel scale: {pixelScale} arcsec/px, window: {WindowMinutes} min, dither settle: {DitherSettleSeconds} s");
        }

        public void Stop() {
            if (guiderMediator != null) {
                guiderMediator.GuideEvent -= OnGuideEvent;
                guiderMediator.AfterDither -= OnAfterDither;
                Logger.Debug("GuideErrorMonitor stopped");
            }
        }

        public void Clear() {
            Logger.Debug("GuideErrorMonitor clearing buffer and RMS data");
            lock (syncLock) {
                buffer.Clear();
                rms.Clear();
                lastDitherTime = null;
            }
            RaisePropertyChanged(nameof(RmsRA));
            RaisePropertyChanged(nameof(RmsDec));
            RaisePropertyChanged(nameof(RmsTotal));
            RaisePropertyChanged(nameof(DataPoints));
        }

        internal Task OnAfterDither(object sender, EventArgs e) {
            lock (syncLock) {
                lastDitherTime = DateTime.UtcNow;
            }
            Logger.Debug($"GuideErrorMonitor dither detected - ignoring guide events for {DitherSettleSeconds} s");
            return Task.CompletedTask;
        }

        internal void OnGuideEvent(object sender, IGuideStep step) {
            var now = DateTime.UtcNow;
            var cutoff = now.AddMinutes(-WindowMinutes);

            lock (syncLock) {
                // Skip guide events during post-dither settle grace period
                if (lastDitherTime.HasValue) {
                    var elapsed = (now - lastDitherTime.Value).TotalSeconds;
                    if (elapsed < DitherSettleSeconds) {
                        Logger.Debug($"GuideErrorMonitor skipping post-dither guide event ({elapsed:F1} s of {DitherSettleSeconds:F0} s grace period)");
                        return;
                    }
                    lastDitherTime = null;
                }

                // Add the data point
                double ra = step.RADistanceRaw;
                double dec = step.DECDistanceRaw;
                buffer.Add((now, ra, dec));
                rms.AddDataPoint(ra, dec);

                // Trim old entries
                int trimmed = 0;
                while (buffer.Count > 0 && buffer[0].timestamp < cutoff) {
                    var old = buffer[0];
                    buffer.RemoveAt(0);
                    rms.RemoveDataPoint(old.raDist, old.decDist);
                    trimmed++;
                }

                Logger.Debug($"GuideErrorMonitor guide event - RA: {ra:F3}, Dec: {dec:F3}, trimmed: {trimmed}, buffer: {buffer.Count} points, RMS total: {rms.Total * rms.Scale:F3} arcsec");
            }

            RaisePropertyChanged(nameof(RmsRA));
            RaisePropertyChanged(nameof(RmsDec));
            RaisePropertyChanged(nameof(RmsTotal));
            RaisePropertyChanged(nameof(DataPoints));
        }
    }
}
