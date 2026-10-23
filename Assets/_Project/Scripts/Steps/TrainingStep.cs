using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Steps
{
    public abstract class TrainingStep
    {
        public StepDefinition Definition { get; }
        protected StepContext Context { get; private set; }

        public float Elapsed { get; private set; }
        public int Mistakes { get; private set; }
        public int HintsUsed { get; private set; }
        public bool IsComplete { get; private set; }
        public bool Passed { get; private set; }
        public bool TimedOut { get; private set; }

        protected bool IsAssessment => Context.Mode == TrainingMode.Assessment;

        protected TrainingStep(StepDefinition definition) { Definition = definition; }

        public void Begin(StepContext context)
        {
            Context = context;
            Elapsed = 0; Mistakes = 0; HintsUsed = 0;
            IsComplete = Passed = TimedOut = false;
            OnBegin();
        }

        public void Tick(float dt)
        {
            if (IsComplete) return;
            Elapsed += dt;
            OnTick(dt);

            float limit = Definition.timeLimitSeconds;
            if (!IsAssessment || limit <= 0) return;
            EventBus.Publish(new StepTimerEvent { Remaining = limit - Elapsed, Limit = limit });
            if (Elapsed >= limit && !IsComplete)
            {
                TimedOut = true;
                Feedback("fb.timeout", FeedbackKind.Warning);
                Finish(false);
            }
        }

        public abstract void HandleInput(StepInput input);

        public string RequestHint()
        {
            if (IsAssessment || IsComplete) return null;
            HintsUsed++;
            return CurrentHintKey;
        }

        public virtual StepResult Evaluate() => new StepResult
        {
            stepId = Definition.stepId,
            passed = Passed,
            timeTaken = Elapsed,
            mistakes = Mistakes,
            hintsUsed = HintsUsed,
            timedOut = TimedOut,
            critical = Definition.critical,
            weight = Definition.weight,
            tipKey = Definition.tipKey
        };

        protected virtual void OnBegin() { }
        protected virtual void OnTick(float dt) { }
        protected virtual string CurrentHintKey => Definition.hintKey;

        protected void AddMistake(string messageKey)
        {
            Mistakes++;
            Feedback(messageKey, FeedbackKind.Mistake);
        }

        protected void Feedback(string key, FeedbackKind kind)
            => EventBus.Publish(new StepFeedbackEvent { MessageKey = key, Kind = kind });

        protected void Finish(bool succeeded)
        {
            if (IsComplete) return;
            IsComplete = true;
            Passed = succeeded && Mistakes <= Definition.maxMistakes;
            OnFinished();
        }

        protected virtual void OnFinished() { }
    }
}
