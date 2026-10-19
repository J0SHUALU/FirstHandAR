using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstHand.Data
{
    public enum StepType { HazardScan, ResponseCheck, CallHelp, BreathingCheck, Compressions, RecoveryPosition }
    public enum StepBranch { Always, NotBreathing, Breathing }

    /// <summary>
    /// One step entry in the scenario asset. Every number the rules use lives here so a time limit,
    /// pass threshold or order can be changed without touching code (NFR-12).
    /// </summary>
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
        public int targetCount;          // hazards to find, compressions per round
        public int assessmentRounds = 1; // compression rounds in Assessment (activity diagram: 2)
        public float minValue;           // hold min seconds, rate min
        public float maxValue;           // hold max seconds, rate max
        public float radius;             // compression target radius in metres
        [Range(0, 1)] public float requiredInRange = 0.7f;
        [Range(0, 1)] public float requiredOnTarget = 0.8f;
        public string expectedText;      // number to dial
        public List<string> orderedIds = new List<string>(); // shoulders, info items, recovery order
    }

    [Serializable]
    public class HazardDefinition
    {
        public string hazardId;
        public string addressKey;   // Addressables key for the prefab
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
