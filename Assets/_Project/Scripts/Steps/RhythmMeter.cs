using System.Collections.Generic;

namespace FirstHand.Steps
{
    public class RhythmMeter
    {
        private readonly List<float> tapTimes = new List<float>();
        private readonly int window;

        public RhythmMeter(int window = 4) { this.window = window; }

        public int Count => tapTimes.Count;

        public void AddTap(float time) => tapTimes.Add(time);
        public void Reset() => tapTimes.Clear();

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
