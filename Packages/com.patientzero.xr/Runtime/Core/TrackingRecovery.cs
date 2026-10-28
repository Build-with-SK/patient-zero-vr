using System;

namespace PatientZero.Core
{
    public enum InteractionSource { None, Hand, Controller }
    public readonly struct TrackingDecision
    {
        public readonly InteractionSource Source;
        public readonly bool CancelSelection;
        public TrackingDecision(InteractionSource source, bool cancel) { Source = source; CancelSelection = cancel; }
    }
    // Each hand owns one gate. Any source switch cancels; restoration requires stable tracking.
    public sealed class TrackingRecovery
    {
        private readonly double settleSeconds;
        private InteractionSource candidate, current;
        private double candidateSince, clock;
        public TrackingRecovery(double settleSeconds = .25)
        {
            if (double.IsNaN(settleSeconds) || double.IsInfinity(settleSeconds) || settleSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(settleSeconds));
            this.settleSeconds = settleSeconds;
        }
        public TrackingDecision Step(bool handValid, bool controllerValid, double now, bool focused = true)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < clock)
                return new TrackingDecision(current, false);
            clock = now;
            var desired = !focused ? InteractionSource.None : handValid ? InteractionSource.Hand :
                controllerValid ? InteractionSource.Controller : InteractionSource.None;
            bool cancel = current != InteractionSource.None && current != desired;
            if (cancel) current = InteractionSource.None;
            if (desired != candidate) { candidate = desired; candidateSince = now; }
            if (desired == InteractionSource.None) current = InteractionSource.None;
            else if (now - candidateSince >= settleSeconds) current = desired;
            return new TrackingDecision(current, cancel);
        }
    }
}
