using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstHand.Data
{
    public enum StepType { HazardScan, ResponseCheck, CallHelp, BreathingCheck, Compressions, RecoveryPosition }
    public enum StepBranch { Always, NotBreathing, Breathing }

    [Serializable]
    public class StepDefinition
    {
        public string stepId = "step";
        public StepType type;
        public StepBranch branch = StepBranch.Always;

        [Header("Text keys")]
        public string instructionKey;
        public string hintKey;
        public string tipKey;

        [Header("Scoring")]
        [Tooltip("Points this step contributes to the 100 point score")] public int weight = 15;
        [Tooltip("Assessment fails if a critical step fails")] public bool critical;
        [Tooltip("Assessment time limit in seconds, 0 = none")] public float timeLimitSeconds = 30f;
        [Tooltip("More mistakes than this fails the step")] public int maxMistakes = 2;

        [Header("Step specific values")]
        public int targetCount;
        public int assessmentRounds = 1;
        public float minValue;
        public float maxValue;
        public float radius;
        [Range(0, 1)] public float requiredInRange = 0.7f;
        [Range(0, 1)] public float requiredOnTarget = 0.8f;
        public string expectedText;
        public List<string> orderedIds = new List<string>();
    }

    [Serializable]
    public class HazardDefinition
    {
        public string hazardId;
        public string addressKey;
        public string nameKey;
        public bool isDangerous = true;
    }

    [CreateAssetMenu(menuName = "FirstHand/Scenario Definition", fileName = "Scenario_OfficeCollapse")]
    public class ScenarioDefinition : ScriptableObject
    {
        public string scenarioId = "office_collapse_v1";
        public string titleKey = "scenario.title";
        public string casualtyKey = "Casualty";

        [Range(0, 1)] public float breathingChance = 0.5f;
        public int hazardsPerAttempt = 2;
        public int decoysPerAttempt = 2;
        public int passMark = 80;
        public Vector2 minPlaneSize = new Vector2(1.5f, 1.0f);

        public List<HazardDefinition> hazardPool = new List<HazardDefinition>();
        public List<HazardDefinition> decoyPool = new List<HazardDefinition>();
        public List<StepDefinition> steps = new List<StepDefinition>();
    }
}
