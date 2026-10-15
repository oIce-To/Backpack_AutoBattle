using System;

namespace BackpackAutoBattle.Combat
{
    /// <summary>The sole timing authority for all periodic items, including attack items.</summary>
    public sealed class RuntimeItemCycle
    {
        private readonly Func<double> _time;
        private double _startedAt;
        private double _baseInterval;
        private double _speedRate = 1d;
        public RuntimeItemCycle(double baseInterval, Func<double> time)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _baseInterval = ValidateInterval(baseInterval);
            _startedAt = _time();
            NextActivationAt = _startedAt + IntervalSeconds;
        }
        public double BaseIntervalSeconds => _baseInterval;
        public double IntervalSeconds => Math.Max(0.1d, _baseInterval / _speedRate);
        public double NextActivationAt { get; private set; }
        public double ProgressAt(double time) => Math.Clamp((time - _startedAt) / IntervalSeconds, 0d, 1d);
        public void SetBaseInterval(double value) => ChangeTiming(ValidateInterval(value), _speedRate);
        internal void SetSpeedRate(double rate) => ChangeTiming(_baseInterval, Math.Clamp(rate, 0.05d, 20d));
        internal void CompleteActivation(double time)
        { _startedAt = time; NextActivationAt = time + IntervalSeconds; }
        private void ChangeTiming(double interval, double rate)
        {
            if (interval == _baseInterval && rate == _speedRate) return;
            double now = _time();
            double progress = ProgressAt(now);
            _baseInterval = interval;
            _speedRate = rate;
            _startedAt = now - progress * IntervalSeconds;
            NextActivationAt = now + (1d - progress) * IntervalSeconds;
        }
        private static double ValidateInterval(double value)
            => double.IsNaN(value) || double.IsInfinity(value) ? 2d : Math.Max(0.1d, value);
    }
}
