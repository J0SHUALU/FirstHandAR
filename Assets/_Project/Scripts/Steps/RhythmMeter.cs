using System.Collections.Generic;

namespace FirstHand.Steps
{
    /// <summary>
    /// Encapsulates the compression maths. Tap times are private; callers can only add a tap and read
    /// the current rate or the share of taps that were in range.
    /// </summary>
    public class RhythmMeter
    {
        private readonly List<float> tapTimes = new List<float>();
        private readonly int window;

        public RhythmMeter(int window = 4) { this.window = window; }

        public int Count => tapTimes.Count;

        public void AddTap(float time) => tapTimes.Add(time);
        public void Reset() => tapTimes.Clear();

        /// <summary>Compressions per minute from the average of the last few intervals; 0 until two taps.</summary>
        public float CurrentRate
        {
            get
            {
                int n = tapTimes.Count;
                if (n < 2) return 0f;
                int intervals = System.Math.Min(window, n - 1);
                float span = tapTimes[n - 1] - tapTimes[n - 1 - intervals];
                return span <= 0f ? 0f : 60f * intervals / span;
            }
        }

        /// <summary>Rate of the latest single interval, used to judge each tap.</summary>
        public float LastIntervalRate
        {
            get
            {
                int n = tapTimes.Count;
                if (n < 2) return 0f;
                float dt = tapTimes[n - 1] - tapTimes[n - 2];
                return dt <= 0f ? 0f : 60f / dt;
            }
        }

        /// <summary>Share (0..1) of intervals whose instantaneous rate is between min and max inclusive.</summary>
        public float PercentInRange(float min, float max)
        {
            int n = tapTimes.Count;
            if (n < 2) return 0f;
            int ok = 0;
            for (int i = 1; i < n; i++)
            {
                float dt = tapTimes[i] - tapTimes[i - 1];
                if (dt <= 0f) continue;
                float rate = 60f / dt;
                if (rate >= min && rate <= max) ok++;
            }
            return (float)ok / (n - 1);
        }

        public float AverageRate
        {
            get
            {
                int n = tapTimes.Count;
                if (n < 2) return 0f;
                float span = tapTimes[n - 1] - tapTimes[0];
                return span <= 0f ? 0f : 60f * (n - 1) / span;
            }
        }
    }
}
