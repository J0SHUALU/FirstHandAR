using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FirstHand.Steps;

namespace FirstHand.Scoring
{
    public struct SessionScore
    {
        public int Score;          // 0..100
        public bool Passed;        // always false in Practice (no pass mark)
        public bool HasPassMark;
        public List<string> TipKeys;
    }

    /// <summary>
    /// Strategy: Practice and Assessment score the same step results differently.
    /// The mode picks the strategy; SimulationManager just calls Score.
    /// </summary>
    public interface IScoringStrategy
    {
        SessionScore Score(IReadOnlyList<StepResult> results);
    }

    public abstract class ScoringBase : IScoringStrategy
    {
        protected const float MistakePenalty = 0.15f; // share of a step's weight lost per mistake
        protected const float HintPenalty = 0.10f;

        public abstract SessionScore Score(IReadOnlyList<StepResult> results);

        protected static int Normalise(float earned, int totalWeight)
            => totalWeight <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(100f * earned / totalWeight), 0, 100);

        protected static List<string> Tips(IReadOnlyList<StepResult> results)
            => results.Where(r => !r.passed && !string.IsNullOrEmpty(r.tipKey)).Select(r => r.tipKey).ToList();
    }

    /// <summary>Practice: generous. A failed step still earns half its weight; hints cost a little; no pass mark.</summary>
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

    /// <summary>
    /// Assessment (FR-23, FR-24): a failed step earns nothing, mistakes cost points, and the attempt only
    /// passes with score &gt;= pass mark AND every critical step (call, compressions or recovery) passed.
    /// </summary>
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
