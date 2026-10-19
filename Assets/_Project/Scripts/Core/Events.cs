using UnityEngine;
using FirstHand.Data;
using FirstHand.Simulation;
using FirstHand.Steps;

namespace FirstHand.Core
{
    // ---------- App flow ----------
    public struct StateChangedEvent { public SimStateId Previous; public SimStateId Current; }
    public struct LanguageChangedEvent { public Language Language; }
    public struct SettingsChangedEvent { public float Volume; public float TextScale; }

    // ---------- AR ----------
    public struct PlaneStatusEvent { public int PlaneCount; public bool LargestBigEnough; public bool ScanTimedOut; }
    public struct ScenePlacedEvent { public Pose Pose; }
    public struct PlacementRejectedEvent { public string ReasonKey; }
    public struct TrackingChangedEvent { public bool IsTracking; }

    // ---------- Raw input (InputManager / UI publish) ----------
    public struct ScreenTapEvent { public Vector2 ScreenPosition; }
    public struct HoldStartedEvent { public string ButtonId; }
    public struct TapInputEvent { public string TargetId; public Vector3 WorldPoint; public bool HitInteractable; }
    public struct DragEndedEvent { public string LimbId; public bool ReachedTarget; }
    public struct HoldEndedEvent { public string ButtonId; public float Seconds; }
    public struct UiActionEvent { public string Action; public string Value; }

    // ---------- Step progress (steps publish) ----------
    public struct StepStartedEvent { public string StepId; public string InstructionKey; public int Index; public int Total; public bool ShowHint; }
    public struct StepFeedbackEvent { public string MessageKey; public FeedbackKind Kind; }
    public struct StepCompletedEvent { public StepResult Result; }
    public struct StepTimerEvent { public float Remaining; public float Limit; }
    public struct HintShownEvent { public string HintKey; }
    public struct RateUpdatedEvent { public float Rate; public RateBand Band; public int Count; public int Target; public bool OnTarget; }
    public struct HazardFoundEvent { public string HazardId; public int Found; public int Needed; }

    // ---------- Results ----------
    public struct AttemptFinishedEvent { public SessionRecord Record; }
    public struct SessionSavedEvent { public string SessionId; }

    public enum FeedbackKind { Info, Success, Mistake, Warning }
    public enum RateBand { None, TooSlow, Good, TooFast }
}
