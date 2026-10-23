using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Steps
{
    /// <summary>Step 1 (FR-12): find and tap the hazards picked for this attempt; tapping anything else is a mistake.</summary>
    public class HazardScanStep : TrainingStep
    {
        private readonly HashSet<string> found = new HashSet<string>();
        public HazardScanStep(StepDefinition def) : base(def) { }

        protected override void OnBegin() => found.Clear();

        protected override string CurrentHintKey => found.Count == 0 ? Definition.hintKey : "hint.hazards_one_more";

        public override void HandleInput(StepInput input)
        {
            if (!(input is TapInput tap) || !tap.HitInteractable) return;
            if (Context.HazardIds.Contains(tap.TargetId))
            {
                if (!found.Add(tap.TargetId)) return;
                int needed = Mathf.Min(Definition.targetCount, Context.HazardIds.Count);
                EventBus.Publish(new HazardFoundEvent { HazardId = tap.TargetId, Found = found.Count, Needed = needed });
                Feedback("fb.hazard_found", FeedbackKind.Success);
                if (found.Count >= needed) Finish(true);
            }
            else
            {
                AddMistake("fb.not_a_hazard");
            }
        }
    }

    /// <summary>Step 2 (FR-13): tap both shoulders, then shout. Shouting first is a mistake.</summary>
    public class ResponseCheckStep : TrainingStep
    {
        private readonly HashSet<string> tapped = new HashSet<string>();
        public ResponseCheckStep(StepDefinition def) : base(def) { }

        private bool ShouldersDone => Definition.orderedIds.All(tapped.Contains);
        protected override void OnBegin() => tapped.Clear();
        protected override string CurrentHintKey => ShouldersDone ? "hint.shout" : Definition.hintKey;

        public override void HandleInput(StepInput input)
        {
            switch (input)
            {
                case TapInput tap when Definition.orderedIds.Contains(tap.TargetId):
                    if (tapped.Add(tap.TargetId)) Feedback("fb.shoulder_tapped", FeedbackKind.Info);
                    break;
                case ActionInput a when a.Action == "shout":
                    if (ShouldersDone)
                    {
                        Feedback("fb.no_response", FeedbackKind.Success);
                        Finish(true);
                    }
                    else AddMistake("fb.tap_shoulders_first");
                    break;
            }
        }
    }

    /// <summary>Step 3 (FR-14): dial the emergency number, then send exactly the three key facts.</summary>
    public class CallHelpStep : TrainingStep
    {
        private string dialled = "";
        private bool connected;
        private readonly HashSet<string> selected = new HashSet<string>();

        public CallHelpStep(StepDefinition def) : base(def) { }
        public bool IsConnected => connected;

        protected override void OnBegin() { dialled = ""; connected = false; selected.Clear(); }
        protected override string CurrentHintKey => connected ? "hint.call_info" : Definition.hintKey;

        public override void HandleInput(StepInput input)
        {
            if (!(input is ActionInput a)) return;
            switch (a.Action)
            {
                case "dial" when !connected && dialled.Length < 6:
                    dialled += a.Value;
                    break;
                case "dial_clear":
                    dialled = "";
                    break;
                case "call" when !connected:
                    if (dialled == Definition.expectedText)
                    {
                        connected = true;
                        Feedback("fb.call_connected", FeedbackKind.Success);
                    }
                    else AddMistake("fb.wrong_number");
                    dialled = connected ? dialled : "";
                    break;
                case "info" when connected:
                    if (!selected.Remove(a.Value)) selected.Add(a.Value);
                    break;
                case "send_info" when connected:
                    var required = new HashSet<string>(Definition.orderedIds);
                    if (selected.SetEquals(required))
                    {
                        Feedback("fb.help_coming", FeedbackKind.Success);
                        Finish(true);
                    }
                    else
                    {
                        AddMistake(selected.IsSubsetOf(required) ? "fb.info_missing" : "fb.info_wrong");
                    }
                    break;
            }
        }
    }

    /// <summary>Step 4 (FR-15): hold Check Breathing for 5 to 10 seconds.</summary>
    public class BreathingCheckStep : TrainingStep
    {
        public BreathingCheckStep(StepDefinition def) : base(def) { }

        public override void HandleInput(StepInput input)
        {
            if (!(input is HoldInput hold) || hold.ButtonId != "check_breathing") return;
            if (hold.Seconds < Definition.minValue) AddMistake("fb.hold_short");
            else if (hold.Seconds > Definition.maxValue) AddMistake("fb.hold_long");
            else
            {
                Feedback(Context.CasualtyBreathing ? "fb.is_breathing" : "fb.not_breathing", FeedbackKind.Success);
                Finish(true);
            }
        }
    }

    /// <summary>
    /// Step 5a (FR-16, FR-17): rhythm taps on the chest. Each tap updates the meter; a round passes when
    /// enough intervals were 100 to 120 per minute and enough taps landed on the lower breastbone.
    /// </summary>
    public class CompressionStep : TrainingStep
    {
        private readonly RhythmMeter meter = new RhythmMeter();
        private int onTarget, roundTaps, round, roundsPassed, rounds;
        private float sumInRange, sumOnTarget, sumRate;

        public CompressionStep(StepDefinition def) : base(def) { }

        protected override void OnBegin()
        {
            meter.Reset();
            onTarget = roundTaps = round = roundsPassed = 0;
            sumInRange = sumOnTarget = sumRate = 0;
            rounds = IsAssessment ? Mathf.Max(1, Definition.assessmentRounds) : 1;
        }

        public override void HandleInput(StepInput input)
        {
            if (!(input is TapInput tap) || !tap.HitInteractable) return;
            if (!tap.TargetId.StartsWith("casualty")) return;

            bool hit = Vector3.Distance(tap.WorldPoint, Context.Casualty.ChestTarget) <= Definition.radius;
            if (hit) onTarget++;
            roundTaps++;
            meter.AddTap(Elapsed);

            float rate = meter.LastIntervalRate;
            var band = meter.Count < 2 ? RateBand.None
                     : rate < Definition.minValue ? RateBand.TooSlow
                     : rate > Definition.maxValue ? RateBand.TooFast : RateBand.Good;
            EventBus.Publish(new RateUpdatedEvent
            {
                Rate = meter.CurrentRate, Band = band, Count = roundTaps, Target = Definition.targetCount, OnTarget = hit
            });

            if (roundTaps >= Definition.targetCount) CloseRound();
        }

        private void CloseRound()
        {
            float inRange = meter.PercentInRange(Definition.minValue, Definition.maxValue);
            float onTgt = (float)onTarget / roundTaps;
            sumInRange += inRange; sumOnTarget += onTgt; sumRate += meter.AverageRate;
            bool ok = inRange >= Definition.requiredInRange && onTgt >= Definition.requiredOnTarget;
            if (ok) roundsPassed++;
            else if (inRange < Definition.requiredInRange) AddMistake("fb.rhythm_off");
            else AddMistake("fb.hands_off_target");

            round++;
            meter.Reset(); onTarget = 0; roundTaps = 0;
            if (round >= rounds) Finish(roundsPassed == rounds);
            else Feedback("fb.next_round", FeedbackKind.Info);
        }

        public override StepResult Evaluate()
        {
            var r = base.Evaluate();
            int done = Mathf.Max(1, round);
            r.percentInRange = sumInRange / done;
            r.percentOnTarget = sumOnTarget / done;
            r.averageRate = sumRate / done;
            return r;
        }
    }

    /// <summary>Step 5b (FR-18): drag limbs in the correct order; a limb moved out of order snaps back.</summary>
    public class RecoveryPositionStep : TrainingStep
    {
        private int next;
        public RecoveryPositionStep(StepDefinition def) : base(def) { }

        protected override void OnBegin()
        {
            next = 0;
            Context.Casualty?.SetRecoveryStage(0);
            Context.Casualty?.SetLimbHandlesActive(true);
            stageShown = false;
        }

        private bool stageShown;

        // Shown on the first tick: StepStartedEvent (sent after OnBegin) would otherwise overwrite it.
        protected override void OnTick(float dt)
        {
            if (stageShown) return;
            stageShown = true;
            ShowStage();
        }

        /// <summary>The instruction card always says which limb to move next.</summary>
        private void ShowStage()
        {
            if (next < Definition.orderedIds.Count)
                EventBus.Publish(new InstructionChangedEvent { InstructionKey = "hint.recovery." + Definition.orderedIds[next] });
        }

        protected override string CurrentHintKey => "hint.recovery." + Definition.orderedIds[Mathf.Min(next, Definition.orderedIds.Count - 1)];

        public override void HandleInput(StepInput input)
        {
            if (!(input is DragInput drag)) return;
            if (!drag.ReachedTarget)
            {
                Context.Casualty?.SnapLimbBack(drag.LimbId);
                Feedback("fb.drag_further", FeedbackKind.Info);
                return;
            }
            if (drag.LimbId == Definition.orderedIds[next])
            {
                next++;
                Context.Casualty?.SetRecoveryStage(next);
                Feedback("fb.limb_ok", FeedbackKind.Success);
                if (next >= Definition.orderedIds.Count) Finish(true);
                else ShowStage();
            }
            else
            {
                Context.Casualty?.SnapLimbBack(drag.LimbId);
                AddMistake("fb.wrong_order");
            }
        }

        protected override void OnFinished() => Context.Casualty?.SetLimbHandlesActive(false);
    }
}
