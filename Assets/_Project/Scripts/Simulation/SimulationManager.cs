using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using FirstHand.AR;
using FirstHand.Core;
using FirstHand.Data;
using FirstHand.Entities;
using FirstHand.Scoring;
using FirstHand.Steps;

namespace FirstHand.Simulation
{
    public enum SimStateId { Start, Scanning, Placement, ModeSelect, Running, Paused, Results }

    public class SimulationManager : Singleton<SimulationManager>
    {
        [SerializeField] private ScenarioDefinition scenario;
        [SerializeField] private Camera arCamera;
        [SerializeField] private float trackingLossPauseSeconds = 2f;

        private readonly Dictionary<SimStateId, ISimState> states = new Dictionary<SimStateId, ISimState>();
        private ISimState current;
        private System.Random rng = new System.Random();

        public ScenarioDefinition Scenario => scenario;
        public Camera ArCamera => arCamera;
        public float TrackingLossPauseSeconds => trackingLossPauseSeconds;
        public SimStateId CurrentState => current?.Id ?? SimStateId.Start;

        public StepManager Steps { get; } = new StepManager();
        public TrainingMode Mode { get; private set; }
        public SessionRecord Record { get; private set; }
        public SessionScore LastScore { get; private set; }
        public Transform SceneRoot { get; private set; }
        public bool SceneLoaded { get; private set; }
        public bool Busy { get; private set; }

        private float attemptStart;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            Application.targetFrameRate = 30;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Register(new StartState(this));
            Register(new ScanningState(this));
            Register(new PlacementState(this));
            Register(new ModeSelectState(this));
            Register(new RunningState(this));
            Register(new PausedState(this));
            Register(new ResultsState(this));

            Steps.StepFinished += OnStepFinished;
            Steps.AllStepsFinished += OnAllStepsFinished;
        }

        private void Start() => ChangeState(SimStateId.Start);

        private void OnEnable() => EventBus.Subscribe<UiActionEvent>(OnUiAction);
        private void OnDisable() => EventBus.Unsubscribe<UiActionEvent>(OnUiAction);

        private void Register(ISimState s) => states[s.Id] = s;

        private void Update() => current?.Tick(Time.deltaTime);

        public void ChangeState(SimStateId next)
        {
            var prev = current;
            prev?.Exit();
            current = states[next];
            current.Enter();
            EventBus.Publish(new StateChangedEvent { Previous = prev?.Id ?? SimStateId.Start, Current = next });
        }

        private void OnUiAction(UiActionEvent e)
        {
            if (Busy) return;
            current?.HandleAction(e.Action, e.Value);
        }

        public async Task<bool> PlaceSceneAsync(Vector2 screenPos)
        {
            if (Busy) return false;
            if (SceneRoot != null) SceneRoot.SetParent(null, true);
            var result = ARManager.Instance.TryPlace(screenPos, arCamera.transform, out _);
            if (result != ARManager.PlaceResult.Placed && SceneRoot != null && ARManager.Instance.AnchorTransform != null)
                SceneRoot.SetParent(ARManager.Instance.AnchorTransform, true);
            if (result == ARManager.PlaceResult.TooSmall)
            {
                EventBus.Publish(new PlacementRejectedEvent { ReasonKey = "place.too_small" });
                return false;
            }
            if (result != ARManager.PlaceResult.Placed) return false;

            if (SceneRoot == null) SceneRoot = new GameObject("FirstHandScene").transform;
            SceneRoot.SetParent(ARManager.Instance.AnchorTransform, false);
            SceneRoot.localPosition = Vector3.zero;
            SceneRoot.localRotation = Quaternion.identity;

            if (!SceneLoaded)
            {
                Busy = true;
                try
                {
                    await EntityManager.Instance.LoadSceneAsync(scenario, SceneRoot);
                    await PrepareAttemptAsync();
                    SceneLoaded = true;
                }
                finally { Busy = false; }
            }
            return true;
        }

        public void RotateScene(float degrees)
        {
            if (SceneRoot != null) SceneRoot.Rotate(0f, degrees, 0f, Space.Self);
        }

        public void UnloadScene()
        {
            Steps.Stop();
            EntityManager.Instance.UnloadScene();
            ARManager.Instance.ResetSession();
            if (SceneRoot != null) Destroy(SceneRoot.gameObject);
            SceneRoot = null;
            SceneLoaded = false;
        }

        private bool nextBreathing;
        private List<HazardDefinition> nextHazards, nextDecoys;

        public async Task PrepareAttemptAsync()
        {
            nextBreathing = rng.NextDouble() < scenario.breathingChance;
            nextHazards = scenario.hazardPool.OrderBy(_ => rng.Next()).Take(scenario.hazardsPerAttempt).ToList();
            nextDecoys = scenario.decoyPool.OrderBy(_ => rng.Next()).Take(scenario.decoysPerAttempt).ToList();
            await EntityManager.Instance.PrepareAttemptAsync(nextBreathing, nextHazards, nextDecoys, rng);
        }

        public async void BeginAttempt(TrainingMode mode)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                if (Record != null) await PrepareAttemptAsync();
            }
            finally { Busy = false; }

            Mode = mode;
            var trainee = DataManager.Instance.CurrentTrainee;
            Record = new SessionRecord
            {
                sessionId = "s-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                traineeId = trainee?.traineeId ?? "guest",
                mode = mode.ToString(),
                scenarioId = scenario.scenarioId,
                branch = nextBreathing ? "breathing" : "not_breathing",
                status = "incomplete",
                startedAt = DataManager.Now(),
                hazards = nextHazards.Select(h => h.hazardId).ToList()
            };
            attemptStart = Time.time;
            if (trainee != null) DataManager.Instance.SaveSession(Record);

            var ctx = new StepContext
            {
                Mode = mode,
                CasualtyBreathing = nextBreathing,
                HazardIds = new HashSet<string>(Record.hazards),
                Casualty = EntityManager.Instance.Casualty
            };
            Steps.Load(scenario, ctx);
            ChangeState(SimStateId.Running);
            Steps.Start();
        }

        private void OnStepFinished(StepResult r)
        {
            Record.steps.Add(r);
            Record.durationSec = Time.time - attemptStart;
            if (DataManager.Instance.CurrentTrainee != null) DataManager.Instance.SaveSession(Record);
        }

        private void OnAllStepsFinished()
        {
            IScoringStrategy strategy = Mode == TrainingMode.Assessment
                ? new AssessmentScoring(scenario.passMark)
                : (IScoringStrategy)new PracticeScoring();

            LastScore = strategy.Score(Steps.Results);
            Record.score = LastScore.Score;
            Record.passed = LastScore.Passed;
            Record.status = "complete";
            Record.durationSec = Time.time - attemptStart;
            if (DataManager.Instance.CurrentTrainee != null) DataManager.Instance.SaveSession(Record);

            EventBus.Publish(new AttemptFinishedEvent { Record = Record });
            ChangeState(SimStateId.Results);
        }

        public void QuitAttempt()
        {
            if (Record == null || Record.status == "complete") return;
            Steps.AbortCurrent();
            Steps.Stop();
            Record.steps = Steps.Results.ToList();
            Record.durationSec = Time.time - attemptStart;
            Record.status = "incomplete";
            if (DataManager.Instance.CurrentTrainee != null) DataManager.Instance.SaveSession(Record);
        }

        public StepType? CurrentStepType => Steps.Current?.Definition.type;

        public void ForgetAttempt() => Record = null;

        private void OnApplicationPause(bool paused)
        {
            if (paused && CurrentState == SimStateId.Running) ChangeState(SimStateId.Paused);
        }
    }
}
