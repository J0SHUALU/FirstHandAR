using System;
using System.Collections.Generic;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Steps
{
    public class StepManager
    {
        private readonly List<TrainingStep> steps = new List<TrainingStep>();
        private readonly List<StepResult> results = new List<StepResult>();
        private StepContext context;
        private int index = -1;
        private bool listening;

        public event Action<StepResult> StepFinished;
        public event Action AllStepsFinished;

        public TrainingStep Current => index >= 0 && index < steps.Count ? steps[index] : null;
        public int Index => index;
        public int Count => steps.Count;
        public IReadOnlyList<StepResult> Results => results;
        public bool IsFinished => index >= steps.Count;
        public bool Paused { get; set; }

        public void Load(ScenarioDefinition scenario, StepContext ctx)
        {
            context = ctx;
            steps.Clear();
            results.Clear();
            index = -1;
            var branch = ctx.CasualtyBreathing ? StepBranch.Breathing : StepBranch.NotBreathing;
            foreach (var def in scenario.steps)
                if (def.branch == StepBranch.Always || def.branch == branch)
                    steps.Add(StepFactory.Create(def));
        }

        public void Start()
        {
            Listen(true);
            Advance();
        }

        public void Stop() => Listen(false);

        public void Tick(float dt)
        {
            if (Paused || Current == null) return;
            Current.Tick(dt);
            CheckComplete();
        }

        public void HandleInput(StepInput input)
        {
            if (Paused || Current == null) return;
            Current.HandleInput(input);
            CheckComplete();
        }

        public void AbortCurrent()
        {
            if (Current == null || Current.IsComplete) return;
            var r = Current.Evaluate();
            r.passed = false;
            results.Add(r);
        }

        private void CheckComplete()
        {
            if (Current == null || !Current.IsComplete) return;
            var result = Current.Evaluate();
            results.Add(result);
            EventBus.Publish(new StepCompletedEvent { Result = result });
            StepFinished?.Invoke(result);
            Advance();
        }

        private void Advance()
        {
            index++;
            if (index >= steps.Count)
            {
                Listen(false);
                AllStepsFinished?.Invoke();
                return;
            }
            var step = steps[index];
            step.Begin(context);
            EventBus.Publish(new StepStartedEvent
            {
                StepId = step.Definition.stepId,
                InstructionKey = step.Definition.instructionKey,
                Index = index,
                Total = steps.Count,
                ShowHint = context.Mode == TrainingMode.Practice
            });
        }

        private void Listen(bool on)
        {
            if (on == listening) return;
            listening = on;
            if (on)
            {
                EventBus.Subscribe<TapInputEvent>(OnTap);
                EventBus.Subscribe<HoldEndedEvent>(OnHold);
                EventBus.Subscribe<UiActionEvent>(OnAction);
                EventBus.Subscribe<DragEndedEvent>(OnDrag);
            }
            else
            {
                EventBus.Unsubscribe<TapInputEvent>(OnTap);
                EventBus.Unsubscribe<HoldEndedEvent>(OnHold);
                EventBus.Unsubscribe<UiActionEvent>(OnAction);
                EventBus.Unsubscribe<DragEndedEvent>(OnDrag);
            }
        }

        private void OnTap(TapInputEvent e) => HandleInput(new TapInput(e.TargetId, e.WorldPoint, e.HitInteractable));
        private void OnHold(HoldEndedEvent e) => HandleInput(new HoldInput(e.ButtonId, e.Seconds));
        private void OnDrag(DragEndedEvent e) => HandleInput(new DragInput(e.LimbId, e.ReachedTarget));

        private void OnAction(UiActionEvent e)
        {
            if (e.Action == "hint")
            {
                var key = Current?.RequestHint();
                if (key != null) EventBus.Publish(new HintShownEvent { HintKey = key });
                return;
            }
            HandleInput(new ActionInput(e.Action, e.Value));
        }
    }
}
