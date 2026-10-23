using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FirstHand.Steps;

namespace FirstHand.Scoring
{
    public struct SessionScore
    {
        public int Score;
        public bool Passed;
        public bool HasPassMark;
        public List<string> TipKeys;
    }

    public interface IScoringStrategy
    {
        SessionScore Score(IReadOnlyList<StepResult> results);
    }

    public abstract class ScoringBase : IScoringStrategy
    {
        protected const float MistakePenalty = 0.15f;
        protected const float HintPenalty = 0.10f;

        public abstract SessionScore Score(IReadOnlyList<StepResult> results);

        protected static int Normalise(float earned, int totalWeight)
            => totalWeight <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(100f * earned / totalWeight), 0, 100);

        protected static List<string> Tips(IReadOnlyList<StepResult> results)
            => results.Where(r => !r.passed && !string.IsNullOrEmpty(r.tipKey)).Select(r => r.tipKey).ToList();
    }

    public class PracticeScoring : ScoringBase
    {
        public override SessionScore Score(IReadOnlyList<StepResult> results)
        {
            float earned = 0; int total = 0;
            foreach (var r in results)
            {
                total += r.weight;
                float share = r.passed ? 1f : 0.5f;
                share -= r.mistakes * MistakePenalty + r.hintsUsed * HintPenalty;
                earned += r.weight * Mathf.Clamp01(share);
            }
            return new SessionScore { Score = Normalise(earned, total), Passed = false, HasPassMark = false, TipKeys = Tips(results) };
        }
    }

    public class AssessmentScoring : ScoringBase
    {
        private readonly int passMark;
        public AssessmentScoring(int passMark) { this.passMark = passMark; }

        public override SessionScore Score(IReadOnlyList<StepResult> results)
        {
            float earned = 0; int total = 0;
            foreach (var r in results)
            {
                total += r.weight;
                if (r.passed) earned += r.weight * Mathf.Clamp01(1f - r.mistakes * MistakePenalty);
            }
            int score = Normalise(earned, total);
            bool criticalOk = results.Where(r => r.critical).All(r => r.passed);
            return new SessionScore { Score = score, Passed = score >= passMark && criticalOk, HasPassMark = true, TipKeys = Tips(results) };
        }
    }
}
