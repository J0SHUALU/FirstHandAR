using System;
using System.Collections.Generic;
using UnityEngine;
using FirstHand.Data;

namespace FirstHand.Steps
{
    /// <summary>Summary of one step that survives the attempt (the per-frame data is thrown away).</summary>
    [Serializable]
    public class StepResult
    {
        public string stepId;
        public bool passed;
        public float timeTaken;
        public int mistakes;
        public int hintsUsed;
        public bool timedOut;
        public bool critical;
        public int weight;
        public string tipKey;
        // Compression detail (0 when not relevant)
        public float percentInRange;
        public float percentOnTarget;
        public float averageRate;
    }

    // ---------- Inputs a step can receive (already translated from touches / UI by managers) ----------
    public abstract class StepInput { }
    public sealed class TapInput : StepInput
    {
        public readonly string TargetId; public readonly Vector3 WorldPoint; public readonly bool HitInteractable;
        public TapInput(string targetId, Vector3 point, bool hit) { TargetId = targetId; WorldPoint = point; HitInteractable = hit; }
    }
    public sealed class HoldInput : StepInput
    {
        public readonly string ButtonId; public readonly float Seconds;
        public HoldInput(string buttonId, float seconds) { ButtonId = buttonId; Seconds = seconds; }
    }
    public sealed class ActionInput : StepInput
    {
        public readonly string Action; public readonly string Value;
        public ActionInput(string action, string value = null) { Action = action; Value = value; }
    }
    public sealed class DragInput : StepInput
    {
        public readonly string LimbId; public readonly bool ReachedTarget;
        public DragInput(string limbId, bool reached) { LimbId = limbId; ReachedTarget = reached; }
    }

    /// <summary>What a step may ask of the casualty, without knowing about MonoBehaviours (testable with a fake).</summary>
    public interface ICasualtyView
    {
        Vector3 ChestTarget { get; }
        void SetRecoveryStage(int completedStages);
        void SnapLimbBack(string limbId);
        void SetLimbHandlesActive(bool active);
    }

    /// <summary>Everything a step needs for one attempt, passed in rather than fetched from singletons.</summary>
    public class StepContext
    {
        public TrainingMode Mode;
        public bool CasualtyBreathing;
        public HashSet<string> HazardIds = new HashSet<string>();
        public ICasualtyView Casualty;
    }
}
