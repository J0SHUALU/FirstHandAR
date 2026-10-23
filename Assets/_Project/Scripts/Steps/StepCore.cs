using System;
using System.Collections.Generic;
using UnityEngine;
using FirstHand.Data;

namespace FirstHand.Steps
{
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
        public float percentInRange;
        public float percentOnTarget;
        public float averageRate;
    }

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

    public interface ICasualtyView
    {
        Vector3 ChestTarget { get; }
        void SetRecoveryStage(int completedStages);
        void SnapLimbBack(string limbId);
        void SetLimbHandlesActive(bool active);
    }

    public class StepContext
    {
        public TrainingMode Mode;
        public bool CasualtyBreathing;
        public HashSet<string> HazardIds = new HashSet<string>();
        public ICasualtyView Casualty;
    }
}
